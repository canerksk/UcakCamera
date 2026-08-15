using KameraIzleme.Saglayicilar;
using Xunit;

namespace KameraIzleme.Tests;

public class SaglayiciTanimYukleyiciTestleri
{
    [Fact]
    public void JSON_dosyalari_yuklenip_ayristirilabiliyor()
    {
        string gecici = Path.Combine(Path.GetTempPath(), "kamera-tanim-" + Guid.NewGuid());
        Directory.CreateDirectory(gecici);
        try
        {
            File.WriteAllText(Path.Combine(gecici, "test.json"), """
                {
                  "marka": "TestMarka",
                  "oncelik": 75,
                  "parmakizi": {
                    "onvif_uretici_icerir": ["test"],
                    "http_server_basligi_icerir": ["Test-Server"],
                    "http_yol_kontrolu": [{ "yol": "/", "beklenen_durum": 200 }]
                  },
                  "kimlik_dogrulama": "basic",
                  "rtsp": { "ana_akis": "/live/{kanal}", "alt_akis": null },
                  "durum_kontrolu": {
                    "yol": "/status",
                    "format": "xml",
                    "basari_kosulu": "//OK"
                  }
                }
                """);

            var y = new SaglayiciTanimYukleyici();
            var liste = y.Yukle(gecici);

            Assert.Single(liste);
            var t = liste[0];
            Assert.Equal("TestMarka", t.Marka);
            Assert.Equal(75, t.Oncelik);
            Assert.Equal("basic", t.KimlikDogrulama);
            Assert.Contains("test", t.Parmakizi.OnvifUreticiIcerir);
            Assert.Equal("/status", t.DurumKontrolu!.Yol);
            Assert.Equal("/live/{kanal}", t.Rtsp!.AnaAkis);
        }
        finally
        {
            Directory.Delete(gecici, recursive: true);
        }
    }

    [Fact]
    public void Hatali_dosya_atlanip_digerleri_yuklenebiliyor()
    {
        string gecici = Path.Combine(Path.GetTempPath(), "kamera-tanim-" + Guid.NewGuid());
        Directory.CreateDirectory(gecici);
        try
        {
            File.WriteAllText(Path.Combine(gecici, "bozuk.json"), "{{{ bozuk");
            File.WriteAllText(Path.Combine(gecici, "iyi.json"),
                "{\"marka\":\"Iyi\",\"oncelik\":10,\"parmakizi\":{}}");

            var y = new SaglayiciTanimYukleyici();
            var liste = y.Yukle(gecici);

            Assert.Single(liste);
            Assert.Equal("Iyi", liste[0].Marka);
        }
        finally
        {
            Directory.Delete(gecici, recursive: true);
        }
    }
}
