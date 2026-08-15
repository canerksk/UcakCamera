using KameraIzleme.Modeller;
using KameraIzleme.Saglayicilar;
using Xunit;

namespace KameraIzleme.Tests;

public class YerTutucuTestleri
{
    [Fact]
    public void Kamera_yer_tutucularini_dogru_cozer()
    {
        var k = new Kamera
        {
            Ip = "10.0.0.1",
            Kullanici = "root",
            HttpPort = 8080,
            RtspPort = 5544,
            OnvifPort = 2020,
        };

        string sablon = "/{ip}/{kullanici}/{kanal}/rtsp:{rtsp_port}/http:{http_port}/onvif:{onvif_port}";
        string cozulmus = YerTutucu.Coz(sablon, YerTutucu.KameraSozlugu(k, "5"));

        Assert.Equal("/10.0.0.1/root/5/rtsp:5544/http:8080/onvif:2020", cozulmus);
    }

    [Fact]
    public void Bilinmeyen_yer_tutucu_oldugu_gibi_kalir()
    {
        var k = new Kamera { Ip = "1.1.1.1" };
        string s = YerTutucu.Coz("/{ip}/{yok}/x", YerTutucu.KameraSozlugu(k));
        Assert.Equal("/1.1.1.1/{yok}/x", s);
    }
}
