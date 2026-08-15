using KameraIzleme.Modeller;
using KameraIzleme.Saglayicilar;
using Xunit;

namespace KameraIzleme.Tests;

public class SaglayiciSeciciTestleri
{
    private sealed class SahteSaglayici : IKameraSaglayici
    {
        public string Marka { get; }
        public int Oncelik { get; }
        public bool DestekVar { get; }
        public bool Basarili { get; set; }

        public SahteSaglayici(string marka, int oncelik, bool destekVar, bool basarili = true)
        {
            Marka = marka;
            Oncelik = oncelik;
            DestekVar = destekVar;
            Basarili = basarili;
        }

        public Task<bool> DesteklerMiAsync(CihazBilgisi cihaz, CancellationToken ct) => Task.FromResult(DestekVar);

        public Task<KontrolSonucu> DurumKontrolAsync(Kamera kamera, CancellationToken ct) =>
            Task.FromResult(new KontrolSonucu { Basarili = Basarili, AktifKatman = Marka });

        public Task<IReadOnlyList<KanalBilgisi>> KanallariGetirAsync(Kamera kamera, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<KanalBilgisi>>(Array.Empty<KanalBilgisi>());

        public async IAsyncEnumerable<CihazOlayi> OlaylariDinleAsync(
            Kamera kamera,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public async Task Eslesme_yoksa_evrensel_seciliyor()
    {
        var evrensel = new SahteSaglayici("evrensel", 0, destekVar: true);
        var marka = new SahteSaglayici("marka", 100, destekVar: false);
        var secici = new SaglayiciSecici(new[] { (IKameraSaglayici)evrensel, marka });

        var k = new Kamera { Ad = "t", Ip = "1.2.3.4" };
        var secilen = await secici.AktifSaglayiciAsync(k, default);

        Assert.Equal("evrensel", secilen.Marka);
    }

    [Fact]
    public async Task En_yuksek_oncelikli_destek_seciliyor()
    {
        var evrensel = new SahteSaglayici("evrensel", 0, destekVar: true);
        var onvif = new SahteSaglayici("onvif", 50, destekVar: true);
        var marka = new SahteSaglayici("marka", 100, destekVar: true);
        var secici = new SaglayiciSecici(new[] { (IKameraSaglayici)evrensel, onvif, marka });

        var k = new Kamera { Ad = "t", Ip = "1.2.3.4" };
        var secilen = await secici.AktifSaglayiciAsync(k, default);

        Assert.Equal("marka", secilen.Marka);
    }

    [Fact]
    public void UcArdisikHatadan_sonra_altKatmanaGeciliyor()
    {
        var evrensel = new SahteSaglayici("evrensel", 0, destekVar: true);
        var marka = new SahteSaglayici("marka", 100, destekVar: true, basarili: false);
        var secici = new SaglayiciSecici(new[] { (IKameraSaglayici)evrensel, marka });

        var k = new Kamera { Id = 1, Ad = "t", Ip = "1.2.3.4", Saglayici = "marka" };

        // 1., 2. hata — hâlâ marka
        var s1 = secici.SonucuIslegetVeSaglayiciyiSec(k, marka, new KontrolSonucu { Basarili = false });
        Assert.Equal("marka", s1.Marka);
        var s2 = secici.SonucuIslegetVeSaglayiciyiSec(k, marka, new KontrolSonucu { Basarili = false });
        Assert.Equal("marka", s2.Marka);
        // 3. hatada evrensel'e düşmeli
        var s3 = secici.SonucuIslegetVeSaglayiciyiSec(k, marka, new KontrolSonucu { Basarili = false });
        Assert.Equal("evrensel", s3.Marka);
    }

    [Fact]
    public void ElleSecildiyse_dusme_uygulanmiyor()
    {
        var evrensel = new SahteSaglayici("evrensel", 0, destekVar: true);
        var marka = new SahteSaglayici("marka", 100, destekVar: true, basarili: false);
        var secici = new SaglayiciSecici(new[] { (IKameraSaglayici)evrensel, marka });

        var k = new Kamera { Id = 2, Ad = "t", Ip = "1.2.3.5", Saglayici = "marka", SaglayiciElleSecildi = true };

        for (int i = 0; i < 5; i++)
        {
            var s = secici.SonucuIslegetVeSaglayiciyiSec(k, marka, new KontrolSonucu { Basarili = false });
            Assert.Equal("marka", s.Marka);
        }
    }
}
