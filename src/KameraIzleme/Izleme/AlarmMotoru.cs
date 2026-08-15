using System.Collections.Concurrent;
using System.Text;
using KameraIzleme.Mail;
using KameraIzleme.Modeller;
using KameraIzleme.Veri;
using Serilog;

namespace KameraIzleme.Izleme;

/// <summary>
/// Kesinti / düzelme kararlarını, mail eşiklerini ve toplu kesinti tespiti mantığını yürütür.
/// Bir kontrol turu bittiğinde <see cref="TuruIsleAsync"/> çağrılır.
/// </summary>
public sealed class AlarmMotoru
{
    private readonly AyarlarDeposu _ayarlar;
    private readonly OlayDeposu _olaylar;
    private readonly DurumDeposu _durumlar;
    private readonly KameraDeposu _kameralar;
    private readonly MailGondericisi _mail;

    /// <summary>Kamera → son mail gönderim zamanı (tekrar bildirim aralığı için).</summary>
    private readonly ConcurrentDictionary<long, DateTime> _sonMailUtc = new();

    public AlarmMotoru(
        AyarlarDeposu ayarlar,
        OlayDeposu olaylar,
        DurumDeposu durumlar,
        KameraDeposu kameralar,
        MailGondericisi mail)
    {
        _ayarlar = ayarlar;
        _olaylar = olaylar;
        _durumlar = durumlar;
        _kameralar = kameralar;
        _mail = mail;
    }

    /// <summary>
    /// Bir turdan gelen sonuçları işler: durum güncellemesi, olay açma/kapama, mail gönderimi.
    /// Toplu kesinti tespiti burada devreye girer.
    /// </summary>
    public async Task TuruIsleAsync(
        IReadOnlyList<(Kamera Kamera, KontrolSonucu Sonuc, KameraDurumu OncekiDurum, KameraDurumu YeniDurum)> turSonuclari,
        CancellationToken ct)
    {
        int esik = _ayarlar.Al("alarm.ardisik_hata_esigi", 3);
        int tekrarDakika = _ayarlar.Al("alarm.tekrar_bildirim_dakika", 60);
        double topluYuzde = _ayarlar.Al("alarm.toplu_kesinti_yuzde", 50.0);

        // 1) Yeni düşenler + düzelenler listesi
        var yeniDusenler = new List<(Kamera Kamera, KontrolSonucu Sonuc)>();
        var yeniDuzelenler = new List<(Kamera Kamera, KameraDurumu Onceki)>();

        foreach (var (k, s, onceki, yeni) in turSonuclari)
        {
            if (!s.Basarili)
            {
                if (yeni.ArdisikHata == esik) // eşiğe tam ulaştığı tur
                {
                    yeniDusenler.Add((k, s));
                }
            }
            else if (onceki is not null && !onceki.Cevrimici)
            {
                yeniDuzelenler.Add((k, onceki));
            }
        }

        // 2) Toplu kesinti tespiti
        int toplam = turSonuclari.Count;
        bool topluMu = toplam > 0 && (yeniDusenler.Count * 100.0 / toplam) >= topluYuzde;

        // 3) Bireysel olay kayıtları — HER durumda açılır/kapanır
        foreach (var (k, s) in yeniDusenler)
        {
            var acik = _olaylar.AcikOlay(k.Id);
            if (acik is null)
            {
                _olaylar.AcikOlayAc(new KameraOlayi
                {
                    KameraId = k.Id,
                    Tip = s.OlayTipi ?? OlayTipleri.Dustu,
                    Kaynak = s.AktifKatman,
                    Mesaj = s.Mesaj,
                    Basladi = DateTime.UtcNow,
                });
            }
        }

        foreach (var (k, _) in yeniDuzelenler)
        {
            var acik = _olaylar.AcikOlay(k.Id);
            if (acik is not null)
            {
                _olaylar.OlayKapat(acik.Id, DateTime.UtcNow);
            }
        }

        // 4) Mail: toplu ise tek mail, değilse tek tek + tekrar aralığı kontrolü
        if (topluMu && yeniDusenler.Count > 0)
        {
            await TopluKesintiMailiAsync(yeniDusenler, ct).ConfigureAwait(false);
        }
        else
        {
            foreach (var (k, s) in yeniDusenler)
            {
                await KameraDustuMailiAsync(k, s, tekrarDakika, ct).ConfigureAwait(false);
            }
        }

        foreach (var (k, onceki) in yeniDuzelenler)
        {
            await KameraDuzeldiMailiAsync(k, onceki, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Cihazın kendi sinyal kaybı bildirimi (eşik beklemeden anında mail).
    /// </summary>
    public async Task CihazOlayiIsleAsync(Kamera kamera, CihazOlayi olay, CancellationToken ct)
    {
        // Olayı kaydet
        var acik = _olaylar.AcikOlay(kamera.Id, olay.Tip);
        if (acik is null)
        {
            _olaylar.AcikOlayAc(new KameraOlayi
            {
                KameraId = kamera.Id,
                Tip = olay.Tip,
                Kaynak = "olay_akisi",
                Mesaj = olay.Mesaj,
                Basladi = olay.Zaman,
            });
        }

        // Eşik beklemeden mail
        string konu = $"[SİNYAL KAYBI] {kamera.Ad} ({kamera.Ip})";
        string govde = $"""
            Cihazın kendi teşhisi bildirim gönderdi.

            Kamera : {kamera.Ad}
            IP     : {kamera.Ip}
            Tip    : {olay.Tip}
            Zaman  : {olay.Zaman:yyyy-MM-dd HH:mm:ss} UTC
            Mesaj  : {olay.Mesaj}
            """;
        await _mail.GonderAsync(konu, govde, ct).ConfigureAwait(false);
    }

    private async Task KameraDustuMailiAsync(Kamera k, KontrolSonucu s, int tekrarDakika, CancellationToken ct)
    {
        if (_sonMailUtc.TryGetValue(k.Id, out var son) &&
            (DateTime.UtcNow - son).TotalMinutes < tekrarDakika)
        {
            return;
        }

        string konu = $"[KESİNTİ] {k.Ad} ({k.Ip})";
        string govde = $"""
            Kamera kontrol turunda başarısız oldu.

            Kamera  : {k.Ad}
            IP      : {k.Ip}
            Lokasyon: {k.Lokasyon}
            Katman  : {s.AktifKatman}
            Mesaj   : {s.Mesaj}
            Zaman   : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            """;
        if (await _mail.GonderAsync(konu, govde, ct).ConfigureAwait(false))
        {
            _sonMailUtc[k.Id] = DateTime.UtcNow;
        }
    }

    private async Task KameraDuzeldiMailiAsync(Kamera k, KameraDurumu onceki, CancellationToken ct)
    {
        _sonMailUtc.TryRemove(k.Id, out _);

        TimeSpan kesintiSuresi = DateTime.UtcNow - (onceki.SonBasariliKontrol ?? DateTime.UtcNow);
        string konu = $"[DÜZELDİ] {k.Ad} ({k.Ip})";
        string govde = $"""
            Kamera tekrar erişilebilir.

            Kamera        : {k.Ad}
            IP            : {k.Ip}
            Kesinti süresi: {kesintiSuresi:hh\:mm\:ss}
            Zaman         : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            """;
        await _mail.GonderAsync(konu, govde, ct).ConfigureAwait(false);
    }

    private async Task TopluKesintiMailiAsync(
        IReadOnlyList<(Kamera Kamera, KontrolSonucu Sonuc)> yeniDusenler,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bu turda birden çok kamera aynı anda kesildi (toplu kesinti eşiği aşıldı).");
        sb.AppendLine();
        sb.AppendLine("Etkilenen kameralar:");
        foreach (var (k, s) in yeniDusenler.OrderBy(x => x.Kamera.Lokasyon))
        {
            sb.AppendLine($"  - {k.Ad} ({k.Ip}) [{k.Lokasyon}] — {s.Mesaj}");
        }

        string konu = $"[TOPLU KESİNTİ] {yeniDusenler.Count} kamera aynı anda düştü";
        await _mail.GonderAsync(konu, sb.ToString(), ct).ConfigureAwait(false);
    }
}
