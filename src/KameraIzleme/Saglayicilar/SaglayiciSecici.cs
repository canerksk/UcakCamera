using System.Collections.Concurrent;
using KameraIzleme.Modeller;
using KameraIzleme.Veri;
using Serilog;

namespace KameraIzleme.Saglayicilar;

/// <summary>
/// Kayıtlı sağlayıcıları tutar; parmak izine göre en uygun olanı seçer,
/// kontrol turlarında ardışık başarısızlıkta kademeli olarak bir alt katmana düşer.
/// </summary>
public sealed class SaglayiciSecici
{
    private readonly List<IKameraSaglayici> _sagalyicilar; // öncelik büyükten küçüğe sıralı
    private readonly ConcurrentDictionary<long, SecimDurumu> _durumlar = new();

    public SaglayiciSecici(IEnumerable<IKameraSaglayici> sagalyicilar)
    {
        _sagalyicilar = sagalyicilar.OrderByDescending(s => s.Oncelik).ToList();
    }

    /// <summary>Bilinen tüm sağlayıcı adları (dropdown için).</summary>
    public IReadOnlyList<string> BilinenSaglayiciAdlari =>
        _sagalyicilar.Select(s => s.Marka).ToList();

    public IReadOnlyList<IKameraSaglayici> Tumu => _sagalyicilar;

    public IKameraSaglayici AdIleGetir(string ad) =>
        _sagalyicilar.FirstOrDefault(s => string.Equals(s.Marka, ad, StringComparison.OrdinalIgnoreCase))
            ?? _sagalyicilar.Last(); // fallback = evrensel (en düşük öncelik)

    /// <summary>
    /// Bir kamera için aktif sağlayıcıyı döner. Kamera daha önce elle sabitlenmişse
    /// o kullanılır; aksi hâlde parmak izi taramasıyla tespit yapılır.
    /// </summary>
    public async Task<IKameraSaglayici> AktifSaglayiciAsync(
        Kamera kamera,
        CancellationToken ct)
    {
        if (kamera.SaglayiciElleSecildi && !string.IsNullOrEmpty(kamera.Saglayici))
        {
            return AdIleGetir(kamera.Saglayici);
        }

        // Daha önce seçilmişse tekrar tespite girme.
        if (!string.Equals(kamera.Saglayici, "evrensel", StringComparison.OrdinalIgnoreCase))
        {
            return AdIleGetir(kamera.Saglayici);
        }

        // Tespit — parmak izini topla, ilk destekleyeni seç.
        var cihaz = new CihazBilgisi
        {
            Ip = kamera.Ip,
            HttpPort = kamera.HttpPort,
            OnvifPort = kamera.OnvifPort,
            Kullanici = kamera.Kullanici,
            Sifre = SifreKorumasi.Coz(kamera.SifreSifreli),
        };

        foreach (var s in _sagalyicilar)
        {
            try
            {
                if (await s.DesteklerMiAsync(cihaz, ct).ConfigureAwait(false))
                {
                    Log.Information("Kamera {Ip} için sağlayıcı seçildi: {S}", kamera.Ip, s.Marka);
                    return s;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Sağlayıcı desteği kontrolü hata verdi: {S}", s.Marka);
            }
        }

        return _sagalyicilar.Last();
    }

    /// <summary>
    /// Kontrol turunun sonucunu ve gerekirse kademeli düşüşü uygular.
    /// Verilen sağlayıcı ardışık 3 kez başarısız olursa bir alt katmana geçilir.
    /// Belirli aralıklarla üst katman tekrar denenir.
    /// </summary>
    public IKameraSaglayici SonucuIslegetVeSaglayiciyiSec(
        Kamera kamera,
        IKameraSaglayici cariSaglayici,
        KontrolSonucu sonuc)
    {
        var durum = _durumlar.GetOrAdd(kamera.Id, _ => new SecimDurumu());

        if (sonuc.Basarili)
        {
            durum.ArdisikHata = 0;
            durum.SonYuksekDenemeUtc = DateTime.UtcNow;
            return cariSaglayici;
        }

        durum.ArdisikHata++;

        // Kullanıcı elle seçmişse asla düşme; yalnızca hatayı say.
        if (kamera.SaglayiciElleSecildi)
        {
            return cariSaglayici;
        }

        if (durum.ArdisikHata >= 3)
        {
            var altKatman = BirAltKatman(cariSaglayici);
            if (altKatman != cariSaglayici)
            {
                Log.Warning("Kamera {Ip} sağlayıcısı düşürüldü: {Y} → {A}", kamera.Ip, cariSaglayici.Marka, altKatman.Marka);
                durum.ArdisikHata = 0;
                durum.DustuMu = true;
                durum.EskiSaglayici = cariSaglayici;
                return altKatman;
            }
        }

        // Saatte bir yeniden üst katman denenecek
        if (durum.DustuMu && durum.EskiSaglayici is not null
            && DateTime.UtcNow - durum.SonYuksekDenemeUtc > TimeSpan.FromHours(1))
        {
            Log.Information("Kamera {Ip} için üst sağlayıcı yeniden denenecek: {S}", kamera.Ip, durum.EskiSaglayici.Marka);
            durum.SonYuksekDenemeUtc = DateTime.UtcNow;
            var eski = durum.EskiSaglayici;
            durum.DustuMu = false;
            durum.EskiSaglayici = null;
            return eski;
        }

        return cariSaglayici;
    }

    private IKameraSaglayici BirAltKatman(IKameraSaglayici cari)
    {
        var sonrakiler = _sagalyicilar
            .Where(s => s.Oncelik < cari.Oncelik)
            .OrderByDescending(s => s.Oncelik)
            .ToList();
        return sonrakiler.FirstOrDefault() ?? cari;
    }

    private sealed class SecimDurumu
    {
        public int ArdisikHata;
        public bool DustuMu;
        public IKameraSaglayici? EskiSaglayici;
        public DateTime SonYuksekDenemeUtc = DateTime.UtcNow;
    }
}
