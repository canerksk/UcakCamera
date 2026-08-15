using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace KameraIzleme.Saglayicilar.Onvif;

/// <summary>
/// Elle kurulmuş minimum ONVIF SOAP istemcisi. Paket bağımlılığından kaçınıp
/// yalnızca <see cref="HttpClient"/> ile çalışır.
/// </summary>
public sealed class OnvifSoap
{
    private static readonly HttpClient PaylasilanIstemci = HttpIstemciKur();

    public static XNamespace NsSoap => "http://www.w3.org/2003/05/soap-envelope";
    public static XNamespace NsTds => "http://www.onvif.org/ver10/device/wsdl";
    public static XNamespace NsTrt => "http://www.onvif.org/ver10/media/wsdl";
    public static XNamespace NsTev => "http://www.onvif.org/ver10/events/wsdl";
    public static XNamespace NsTt => "http://www.onvif.org/ver10/schema";
    public static XNamespace NsWsa => "http://www.w3.org/2005/08/addressing";

    /// <summary>Verilen ONVIF servis URL'sine POST atar ve dönen SOAP gövdesini XDocument olarak verir.</summary>
    public async Task<XDocument> GonderAsync(
        string url,
        string aksiyon,
        XElement govde,
        string? kullanici,
        string? sifre,
        CancellationToken ct)
    {
        string zarf = SoapZarfiUret(govde, kullanici, sifre);

        using var istek = new HttpRequestMessage(HttpMethod.Post, url);
        istek.Content = new StringContent(zarf, Encoding.UTF8);
        istek.Content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml")
        {
            CharSet = "utf-8",
            Parameters = { new NameValueHeaderValue("action", $"\"{aksiyon}\"") },
        };

        using var cevap = await PaylasilanIstemci.SendAsync(istek, ct).ConfigureAwait(false);
        string metin = await cevap.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!cevap.IsSuccessStatusCode)
        {
            throw new OnvifHatasi($"ONVIF {cevap.StatusCode}: {Kisalt(metin)}");
        }

        return XDocument.Parse(metin);
    }

    private static string SoapZarfiUret(XElement govde, string? kullanici, string? sifre)
    {
        string header = string.Empty;
        if (!string.IsNullOrEmpty(kullanici))
        {
            header = $"<s:Header>{WsSecurity.UsernameTokenXml(kullanici, sifre ?? string.Empty)}</s:Header>";
        }

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="{NsSoap}">
              {header}
              <s:Body>
                {govde}
              </s:Body>
            </s:Envelope>
            """;
    }

    private static string Kisalt(string metin) =>
        metin.Length > 400 ? metin[..400] + "…" : metin;

    private static HttpClient HttpIstemciKur()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
        var istemci = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
        istemci.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("KameraIzleme", "1.0"));
        return istemci;
    }
}

public sealed class OnvifHatasi : Exception
{
    public OnvifHatasi(string mesaj) : base(mesaj) { }
}
