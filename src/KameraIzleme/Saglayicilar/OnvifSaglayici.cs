using System.Runtime.CompilerServices;
using System.Xml.Linq;
using KameraIzleme.Modeller;
using KameraIzleme.Saglayicilar.Onvif;
using KameraIzleme.Veri;
using Serilog;

namespace KameraIzleme.Saglayicilar;

/// <summary>
/// Katman 2: ONVIF Profile S. Profile S destekleyen hemen her marka aynı arayüzü konuşur;
/// bu katman sayesinde çoğu yeni marka hiçbir ek iş gerektirmeden desteklenir.
/// </summary>
public sealed class OnvifSaglayici : IKameraSaglayici
{
    private readonly OnvifSoap _soap = new();

    public string Marka => "onvif";

    public int Oncelik => 50;

    public async Task<bool> DesteklerMiAsync(CihazBilgisi cihaz, CancellationToken ct)
    {
        string url = $"http://{cihaz.Ip}:{cihaz.OnvifPort}/onvif/device_service";
        try
        {
            var govde = new XElement(OnvifSoap.NsTds + "GetDeviceInformation");
            var belge = await _soap.GonderAsync(
                url,
                "http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation",
                govde,
                cihaz.Kullanici,
                cihaz.Sifre,
                ct).ConfigureAwait(false);

            var uretici = belge.Descendants(OnvifSoap.NsTds + "Manufacturer").FirstOrDefault()?.Value;
            return !string.IsNullOrEmpty(uretici);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "ONVIF GetDeviceInformation başarısız: {Ip}", cihaz.Ip);
            return false;
        }
    }

    public async Task<KontrolSonucu> DurumKontrolAsync(Kamera kamera, CancellationToken ct)
    {
        string url = $"http://{kamera.Ip}:{kamera.OnvifPort}/onvif/device_service";
        string? sifre = SifreKorumasi.Coz(kamera.SifreSifreli);
        try
        {
            var govde = new XElement(OnvifSoap.NsTds + "GetSystemDateAndTime");
            await _soap.GonderAsync(
                url,
                "http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime",
                govde,
                kamera.Kullanici,
                sifre,
                ct).ConfigureAwait(false);

            return new KontrolSonucu
            {
                Basarili = true,
                AktifKatman = "onvif",
                Mesaj = "ONVIF GetSystemDateAndTime başarılı",
            };
        }
        catch (OnvifHatasi ohx) when (ohx.Message.Contains("401") || ohx.Message.Contains("NotAuthorized"))
        {
            return new KontrolSonucu
            {
                Basarili = false,
                AktifKatman = "onvif",
                Mesaj = "ONVIF kimlik doğrulama başarısız",
                OlayTipi = OlayTipleri.YetkisizErisim,
            };
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "ONVIF kontrolü başarısız: {Ip}", kamera.Ip);
            return new KontrolSonucu
            {
                Basarili = false,
                AktifKatman = "onvif",
                Mesaj = $"ONVIF hatası: {ex.Message}",
                OlayTipi = OlayTipleri.Dondu,
            };
        }
    }

    /// <summary>Cihaz bilgisini (üretici, model, seri, firmware) döner.</summary>
    public async Task<CihazBilgisi?> CihazBilgisiAlAsync(
        string ip,
        int onvifPort,
        string? kullanici,
        string? sifre,
        CancellationToken ct)
    {
        string url = $"http://{ip}:{onvifPort}/onvif/device_service";
        try
        {
            var govde = new XElement(OnvifSoap.NsTds + "GetDeviceInformation");
            var belge = await _soap.GonderAsync(
                url,
                "http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation",
                govde,
                kullanici,
                sifre,
                ct).ConfigureAwait(false);

            return new CihazBilgisi
            {
                Ip = ip,
                OnvifPort = onvifPort,
                OnvifUretici = belge.Descendants(OnvifSoap.NsTds + "Manufacturer").FirstOrDefault()?.Value,
                OnvifModel = belge.Descendants(OnvifSoap.NsTds + "Model").FirstOrDefault()?.Value,
                OnvifSeriNo = belge.Descendants(OnvifSoap.NsTds + "SerialNumber").FirstOrDefault()?.Value,
                Kullanici = kullanici,
                Sifre = sifre,
            };
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "GetDeviceInformation hatası: {Ip}", ip);
            return null;
        }
    }

    /// <summary>Kameradan RTSP stream URI'sini sorar.</summary>
    public async Task<string?> RtspUrlAlAsync(
        string ip,
        int onvifPort,
        string? kullanici,
        string? sifre,
        CancellationToken ct)
    {
        string mediaUrl = $"http://{ip}:{onvifPort}/onvif/media";
        try
        {
            // Önce profilleri al
            var profillerGovde = new XElement(OnvifSoap.NsTrt + "GetProfiles");
            var profBelge = await _soap.GonderAsync(
                mediaUrl,
                "http://www.onvif.org/ver10/media/wsdl/GetProfiles",
                profillerGovde,
                kullanici,
                sifre,
                ct).ConfigureAwait(false);

            var ilkProfil = profBelge.Descendants(OnvifSoap.NsTrt + "Profiles").FirstOrDefault()
                         ?? profBelge.Descendants(OnvifSoap.NsTt + "Profiles").FirstOrDefault();
            string? profilToken = ilkProfil?.Attribute("token")?.Value;
            if (string.IsNullOrEmpty(profilToken))
            {
                return null;
            }

            // Sonra stream URI iste
            var streamGovde = new XElement(OnvifSoap.NsTrt + "GetStreamUri",
                new XElement(OnvifSoap.NsTrt + "StreamSetup",
                    new XElement(OnvifSoap.NsTt + "Stream", "RTP-Unicast"),
                    new XElement(OnvifSoap.NsTt + "Transport",
                        new XElement(OnvifSoap.NsTt + "Protocol", "RTSP"))),
                new XElement(OnvifSoap.NsTrt + "ProfileToken", profilToken));

            var streamBelge = await _soap.GonderAsync(
                mediaUrl,
                "http://www.onvif.org/ver10/media/wsdl/GetStreamUri",
                streamGovde,
                kullanici,
                sifre,
                ct).ConfigureAwait(false);

            return streamBelge.Descendants(OnvifSoap.NsTt + "Uri").FirstOrDefault()?.Value;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "GetStreamUri hatası: {Ip}", ip);
            return null;
        }
    }

    public Task<IReadOnlyList<KanalBilgisi>> KanallariGetirAsync(Kamera kamera, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<KanalBilgisi>>(Array.Empty<KanalBilgisi>());

    /// <summary>
    /// PullPoint aboneliği ile olay dinler. Abonelik süresi dolmadan yenilenir,
    /// kopunca exponential backoff ile yeniden kurulur.
    /// </summary>
    public async IAsyncEnumerable<CihazOlayi> OlaylariDinleAsync(
        Kamera kamera,
        [EnumeratorCancellation] CancellationToken ct)
    {
        int gecikmeMs = 5000;
        int enFazlaGecikme = 5 * 60 * 1000;
        string olaylarUrl = $"http://{kamera.Ip}:{kamera.OnvifPort}/onvif/events";
        string? sifre = SifreKorumasi.Coz(kamera.SifreSifreli);

        while (!ct.IsCancellationRequested)
        {
            string? aboneUrl = null;
            try
            {
                aboneUrl = await AboneOlAsync(olaylarUrl, kamera.Kullanici, sifre, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "PullPoint aboneliği kurulamadı: {Ip}", kamera.Ip);
            }

            if (aboneUrl is null)
            {
                await Task.Delay(gecikmeMs, ct).ConfigureAwait(false);
                gecikmeMs = Math.Min(gecikmeMs * 2, enFazlaGecikme);
                continue;
            }

            gecikmeMs = 5000; // sıfırla

            while (!ct.IsCancellationRequested)
            {
                List<CihazOlayi> olaylar;
                try
                {
                    olaylar = await OlaylariCekAsync(aboneUrl, kamera.Kullanici, sifre, ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "PullPoint çekme hatası, yeniden abone olunacak: {Ip}", kamera.Ip);
                    break;
                }

                foreach (var olay in olaylar)
                {
                    yield return olay;
                }
            }
        }
    }

    private async Task<string?> AboneOlAsync(string url, string? kullanici, string? sifre, CancellationToken ct)
    {
        var govde = new XElement(OnvifSoap.NsTev + "CreatePullPointSubscription",
            new XElement(OnvifSoap.NsTev + "InitialTerminationTime", "PT10M"));

        var belge = await _soap.GonderAsync(
            url,
            "http://www.onvif.org/ver10/events/wsdl/EventPortType/CreatePullPointSubscriptionRequest",
            govde,
            kullanici,
            sifre,
            ct).ConfigureAwait(false);

        return belge.Descendants(OnvifSoap.NsWsa + "Address").FirstOrDefault()?.Value;
    }

    private async Task<List<CihazOlayi>> OlaylariCekAsync(
        string aboneUrl,
        string? kullanici,
        string? sifre,
        CancellationToken ct)
    {
        var govde = new XElement(OnvifSoap.NsTev + "PullMessages",
            new XElement(OnvifSoap.NsTev + "Timeout", "PT10S"),
            new XElement(OnvifSoap.NsTev + "MessageLimit", "20"));

        var belge = await _soap.GonderAsync(
            aboneUrl,
            "http://www.onvif.org/ver10/events/wsdl/PullPointSubscription/PullMessagesRequest",
            govde,
            kullanici,
            sifre,
            ct).ConfigureAwait(false);

        var sonuc = new List<CihazOlayi>();
        XNamespace wsnt = "http://docs.oasis-open.org/wsn/b-2";

        foreach (var mesaj in belge.Descendants(wsnt + "NotificationMessage"))
        {
            string? topic = mesaj.Descendants(wsnt + "Topic").FirstOrDefault()?.Value ?? string.Empty;
            string tip = TopicdenTipCikart(topic);
            sonuc.Add(new CihazOlayi
            {
                Tip = tip,
                Mesaj = topic,
            });
        }

        return sonuc;
    }

    private static string TopicdenTipCikart(string topic)
    {
        string kucuk = topic.ToLowerInvariant();
        if (kucuk.Contains("signalloss") || kucuk.Contains("videoloss"))
        {
            return OlayTipleri.SinyalKaybi;
        }

        if (kucuk.Contains("imagetooDark", StringComparison.OrdinalIgnoreCase) || kucuk.Contains("toodark"))
        {
            return OlayTipleri.SinyalKaybi;
        }

        if (kucuk.Contains("hardwarefailure") || kucuk.Contains("diskfailure"))
        {
            return OlayTipleri.DonanimHatasi;
        }

        return topic;
    }
}
