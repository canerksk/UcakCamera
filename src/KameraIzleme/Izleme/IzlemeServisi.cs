using System.Collections.Concurrent;
using KameraIzleme.Modeller;
using KameraIzleme.Saglayicilar;
using KameraIzleme.Veri;
using Serilog;

namespace KameraIzleme.Izleme;

/// <summary>
/// Yoklama motoru. WinForms yaşam döngüsünden bağımsızdır; UI'ya sadece event ile bilgi geçer,
/// <c>Control.Invoke</c> çağırmaz. Marshalling formun sorumluluğunda.
/// </summary>
public sealed class IzlemeServisi : IAsyncDisposable
{
    private readonly AyarlarDeposu _ayarlar;
    private readonly KameraDeposu _kameralar;
    private readonly DurumDeposu _durumlar;
    private readonly OlayDeposu _olaylar;
    private readonly SaglayiciSecici _secici;
    private readonly AlarmMotoru _alarm;

    private readonly ConcurrentDictionary<long, CancellationTokenSource> _olayDinleyicileri = new();
    private readonly ConcurrentDictionary<long, Kamera> _kameraOnbellek = new();
    private CancellationTokenSource? _iptalKaynagi;
    private Task? _ana;

    public event EventHandler<DurumGuncellemeArgumani>? DurumGuncellendi;

    public bool Calisiyor => _ana is { IsCompleted: false };

    public IzlemeServisi(
        AyarlarDeposu ayarlar,
        KameraDeposu kameralar,
        DurumDeposu durumlar,
        OlayDeposu olaylar,
        SaglayiciSecici secici,
        AlarmMotoru alarm)
    {
        _ayarlar = ayarlar;
        _kameralar = kameralar;
        _durumlar = durumlar;
        _olaylar = olaylar;
        _secici = secici;
        _alarm = alarm;
    }

    public void Basla()
    {
        if (Calisiyor)
        {
            return;
        }

        _iptalKaynagi = new CancellationTokenSource();
        _ana = Task.Run(() => AnaDonguAsync(_iptalKaynagi.Token));
        Log.Information("İzleme servisi başladı");
    }

    public async Task DurdurAsync()
    {
        if (_iptalKaynagi is null)
        {
            return;
        }

        try
        {
            _iptalKaynagi.Cancel();

            foreach (var iptal in _olayDinleyicileri.Values)
            {
                try
                {
                    iptal.Cancel();
                }
                catch
                {
                    // görmezden gel
                }
            }

            if (_ana is not null)
            {
                try
                {
                    await _ana.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // beklenen
                }
            }
        }
        finally
        {
            _iptalKaynagi.Dispose();
            _iptalKaynagi = null;
            _ana = null;
            Log.Information("İzleme servisi durdu");
        }
    }

    /// <summary>UI'dan tek kameraya anlık kontrol çalıştırılmasına izin verir.</summary>
    public async Task<KontrolSonucu> SimdiKontrolEtAsync(long kameraId, CancellationToken ct)
    {
        var kamera = _kameralar.Getir(kameraId) ?? throw new InvalidOperationException("Kamera bulunamadı");
        var saglayici = await _secici.AktifSaglayiciAsync(kamera, ct).ConfigureAwait(false);
        var sonuc = await saglayici.DurumKontrolAsync(kamera, ct).ConfigureAwait(false);
        // Tek kamera turu olarak alarm motorundan geçir
        var eski = _durumlar.Getir(kameraId);
        var yeni = DurumHesapla(kameraId, eski, saglayici, sonuc);
        _durumlar.KayitEt(yeni);
        if (sonuc.GecikmeMs is int g)
        {
            _durumlar.GecikmeEkle(kameraId, g);
        }

        await _alarm.TuruIsleAsync(new[] { (kamera, sonuc, eski!, yeni) }, ct).ConfigureAwait(false);
        DurumGuncellendi?.Invoke(this, new DurumGuncellemeArgumani(new[] { (kamera, yeni) }));
        return sonuc;
    }

    private async Task AnaDonguAsync(CancellationToken ct)
    {
        int aralikSn = _ayarlar.Al("izleme.aralik_saniye", 30);
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, aralikSn)));

        // İlk turu hemen başlat
        await TurCalistirAsync(ct).ConfigureAwait(false);

        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                // Aralık her turda ayarlardan tazeleyelim (kullanıcı değiştirmişse)
                int yeni = _ayarlar.Al("izleme.aralik_saniye", 30);
                if (yeni != aralikSn && yeni >= 5)
                {
                    aralikSn = yeni;
                    timer.Dispose();
                    timer = new PeriodicTimer(TimeSpan.FromSeconds(aralikSn));
                }

                await TurCalistirAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // beklenen
        }
        finally
        {
            timer.Dispose();
        }
    }

    private async Task TurCalistirAsync(CancellationToken ct)
    {
        List<Kamera> kameralar;
        try
        {
            kameralar = _kameralar.Tumu().Where(k => k.Aktif).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kamera listesi okunamadı, tur atlanıyor");
            return;
        }

        // Olay dinleyicilerini kameralarla senkronlayalım
        SenkronizeEtOlayDinleyicileri(kameralar, ct);

        using var semafor = new SemaphoreSlim(20);
        var sonuclar = new ConcurrentBag<(Kamera, KontrolSonucu, KameraDurumu OncekiDurum, KameraDurumu YeniDurum)>();

        var gorevler = kameralar.Select(async kamera =>
        {
            await semafor.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var saglayici = await _secici.AktifSaglayiciAsync(kamera, ct).ConfigureAwait(false);
                var sonuc = await saglayici.DurumKontrolAsync(kamera, ct).ConfigureAwait(false);

                // Kademeli düşüş kararı
                var yeniSaglayici = _secici.SonucuIslegetVeSaglayiciyiSec(kamera, saglayici, sonuc);
                if (yeniSaglayici != saglayici)
                {
                    // Aynı turda alt katmanla tekrar dene
                    sonuc = await yeniSaglayici.DurumKontrolAsync(kamera, ct).ConfigureAwait(false);
                }

                var oncekiDurum = _durumlar.Getir(kamera.Id);
                var yeniDurum = DurumHesapla(kamera.Id, oncekiDurum, yeniSaglayici, sonuc);
                _durumlar.KayitEt(yeniDurum);

                if (sonuc.GecikmeMs is int g)
                {
                    _durumlar.GecikmeEkle(kamera.Id, g);
                }

                // Aktif katman değiştiyse veya sağlayıcı tespit farklıysa kamerayı güncelle
                if (!string.Equals(kamera.Saglayici, yeniSaglayici.Marka, StringComparison.OrdinalIgnoreCase)
                    && !kamera.SaglayiciElleSecildi)
                {
                    kamera.Saglayici = yeniSaglayici.Marka;
                    _kameralar.Guncelle(kamera);
                }

                sonuclar.Add((kamera, sonuc, oncekiDurum ?? new KameraDurumu { KameraId = kamera.Id }, yeniDurum));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Kamera kontrolünde beklenmedik hata: {Ip}", kamera.Ip);
            }
            finally
            {
                semafor.Release();
            }
        });

        try
        {
            await Task.WhenAll(gorevler).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Turdaki bazı görevlerde hata oluştu");
        }

        var liste = sonuclar.ToList();
        try
        {
            await _alarm.TuruIsleAsync(liste, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Alarm motoru turu işleyemedi");
        }

        DurumGuncellendi?.Invoke(this, new DurumGuncellemeArgumani(
            liste.Select(t => (t.Item1, t.YeniDurum)).ToList()));
    }

    private void SenkronizeEtOlayDinleyicileri(IReadOnlyList<Kamera> kameralar, CancellationToken ct)
    {
        var kameraIdler = kameralar.Select(k => k.Id).ToHashSet();

        // Kaldırılanları iptal et
        foreach (var eski in _olayDinleyicileri.Keys.Where(k => !kameraIdler.Contains(k)).ToList())
        {
            if (_olayDinleyicileri.TryRemove(eski, out var cts))
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch
                {
                    // görmezden gel
                }

                _kameraOnbellek.TryRemove(eski, out _);
            }
        }

        // Yenileri başlat
        foreach (var kamera in kameralar)
        {
            _kameraOnbellek[kamera.Id] = kamera;
            _olayDinleyicileri.GetOrAdd(kamera.Id, id =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _ = Task.Run(() => OlayAkisiDinleAsync(kamera, cts.Token));
                return cts;
            });
        }
    }

    private async Task OlayAkisiDinleAsync(Kamera kamera, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                IKameraSaglayici saglayici = await _secici.AktifSaglayiciAsync(kamera, ct).ConfigureAwait(false);
                await foreach (var olay in saglayici.OlaylariDinleAsync(kamera, ct).ConfigureAwait(false))
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        await _alarm.CihazOlayiIsleAsync(kamera, olay, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Cihaz olayı işlenemedi: {Ip}", kamera.Ip);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Olay dinleyici hata verdi, tekrar deneniyor: {Ip}", kamera.Ip);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private static KameraDurumu DurumHesapla(
        long kameraId,
        KameraDurumu? onceki,
        IKameraSaglayici saglayici,
        KontrolSonucu sonuc)
    {
        var d = new KameraDurumu
        {
            KameraId = kameraId,
            Cevrimici = sonuc.Basarili,
            GecikmeMs = sonuc.GecikmeMs,
            AktifKatman = sonuc.AktifKatman,
            SonKontrol = DateTime.UtcNow,
            SonMesaj = sonuc.Mesaj,
            ArdisikHata = sonuc.Basarili ? 0 : ((onceki?.ArdisikHata ?? 0) + 1),
            SonBasariliKontrol = sonuc.Basarili ? DateTime.UtcNow : onceki?.SonBasariliKontrol,
        };
        return d;
    }

    public async ValueTask DisposeAsync() => await DurdurAsync().ConfigureAwait(false);
}

/// <summary>Bir turdan sonra fırlatılan olay argümanı.</summary>
public sealed class DurumGuncellemeArgumani : EventArgs
{
    public IReadOnlyList<(Kamera Kamera, KameraDurumu Durum)> Guncellenenler { get; }

    public DurumGuncellemeArgumani(IReadOnlyList<(Kamera, KameraDurumu)> guncellenenler)
    {
        Guncellenenler = guncellenenler;
    }
}
