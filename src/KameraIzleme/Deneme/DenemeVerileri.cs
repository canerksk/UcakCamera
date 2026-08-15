using KameraIzleme.Modeller;
using KameraIzleme.Veri;
using Serilog;

namespace KameraIzleme.Deneme;

/// <summary>
/// Deneme modu için örnek kamera/NVR verisi. Bir kısmı erişilemez IP'ler,
/// biri 127.0.0.1 (sahte HTTP/ONVIF sunucularına düşer).
/// </summary>
public static class DenemeVerileri
{
    public static void EkleEksikleriEkle(KameraDeposu kameralar, NvrDeposu nvrlar)
    {
        if (nvrlar.Tumu().Count == 0)
        {
            nvrlar.Ekle(new Nvr { Ad = "Deneme NVR 1", Ip = "192.0.2.1", Marka = "Hikvision" });
            nvrlar.Ekle(new Nvr { Ad = "Deneme NVR 2", Ip = "192.0.2.2", Marka = "Dahua" });
        }

        if (kameralar.Tumu().Count > 0)
        {
            return;
        }

        Log.Information("Deneme kamera seed verisi ekleniyor");

        // 127.0.0.1 — sahte sunucularımıza gelir
        kameralar.Ekle(new Kamera
        {
            Ad = "Lobi (yerel sahte)",
            Ip = "127.0.0.1",
            HttpPort = 18080,
            OnvifPort = 18081,
            Lokasyon = "Deneme / Lobi",
            Marka = "Hikvision",
            Saglayici = "Hikvision",
        });

        // Erişilemez blok (192.0.2.0/24 = TEST-NET-1, RFC 5737)
        for (int i = 10; i < 20; i++)
        {
            kameralar.Ekle(new Kamera
            {
                Ad = $"Depo Kam {i - 9}",
                Ip = $"192.0.2.{i}",
                Lokasyon = i % 2 == 0 ? "Deneme / Depo" : "Deneme / Bahçe",
                Marka = i % 3 == 0 ? "Dahua" : (i % 3 == 1 ? "Hikvision" : "Uniview"),
            });
        }
    }
}
