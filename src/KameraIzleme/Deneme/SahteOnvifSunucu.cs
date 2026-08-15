using System.Net;
using System.Text;
using Serilog;

namespace KameraIzleme.Deneme;

/// <summary>
/// Küçük yerel bir ONVIF SOAP sunucusu. GetDeviceInformation, GetSystemDateAndTime,
/// CreatePullPointSubscription ve PullMessages çağrılarına anlamlı yanıt verir.
/// </summary>
public sealed class SahteOnvifSunucu : IDisposable
{
    private readonly HttpListener _dinleyici = new();
    private CancellationTokenSource? _iptal;
    private Task? _dongu;

    public string OnEk { get; }

    public SahteOnvifSunucu(int port = 18081)
    {
        OnEk = $"http://127.0.0.1:{port}/";
        _dinleyici.Prefixes.Add(OnEk);
    }

    public void Basla()
    {
        if (_dongu is not null)
        {
            return;
        }

        try
        {
            _dinleyici.Start();
        }
        catch (HttpListenerException ex)
        {
            Log.Warning(ex, "Sahte ONVIF sunucusu başlatılamadı");
            return;
        }

        _iptal = new CancellationTokenSource();
        _dongu = Task.Run(() => DinleAsync(_iptal.Token));
        Log.Information("Sahte ONVIF sunucusu ayakta: {U}", OnEk);
    }

    private async Task DinleAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _dinleyici.GetContextAsync().ConfigureAwait(false);
            }
            catch
            {
                break;
            }

            _ = Task.Run(() => IstegiHazirlaAsync(ctx));
        }
    }

    private static async Task IstegiHazirlaAsync(HttpListenerContext ctx)
    {
        try
        {
            string istek;
            using (var okuyucu = new StreamReader(ctx.Request.InputStream))
            {
                istek = await okuyucu.ReadToEndAsync().ConfigureAwait(false);
            }

            string cevap;
            if (istek.Contains("GetDeviceInformation", StringComparison.Ordinal))
            {
                cevap = SoapSar("""
                    <tds:GetDeviceInformationResponse xmlns:tds="http://www.onvif.org/ver10/device/wsdl">
                      <tds:Manufacturer>SahteONVIF</tds:Manufacturer>
                      <tds:Model>DEMO-100</tds:Model>
                      <tds:FirmwareVersion>1.0.0</tds:FirmwareVersion>
                      <tds:SerialNumber>SN000001</tds:SerialNumber>
                      <tds:HardwareId>HW01</tds:HardwareId>
                    </tds:GetDeviceInformationResponse>
                    """);
            }
            else if (istek.Contains("GetSystemDateAndTime", StringComparison.Ordinal))
            {
                cevap = SoapSar("""
                    <tds:GetSystemDateAndTimeResponse xmlns:tds="http://www.onvif.org/ver10/device/wsdl">
                      <tds:SystemDateAndTime>
                        <tt:UTCDateTime xmlns:tt="http://www.onvif.org/ver10/schema">
                          <tt:Time><tt:Hour>12</tt:Hour><tt:Minute>0</tt:Minute><tt:Second>0</tt:Second></tt:Time>
                          <tt:Date><tt:Year>2025</tt:Year><tt:Month>1</tt:Month><tt:Day>1</tt:Day></tt:Date>
                        </tt:UTCDateTime>
                      </tds:SystemDateAndTime>
                    </tds:GetSystemDateAndTimeResponse>
                    """);
            }
            else if (istek.Contains("CreatePullPointSubscription", StringComparison.Ordinal))
            {
                cevap = SoapSar("""
                    <tev:CreatePullPointSubscriptionResponse xmlns:tev="http://www.onvif.org/ver10/events/wsdl" xmlns:wsa="http://www.w3.org/2005/08/addressing">
                      <tev:SubscriptionReference>
                        <wsa:Address>http://127.0.0.1:18081/subscriptions/1</wsa:Address>
                      </tev:SubscriptionReference>
                    </tev:CreatePullPointSubscriptionResponse>
                    """);
            }
            else if (istek.Contains("PullMessages", StringComparison.Ordinal))
            {
                cevap = SoapSar("""
                    <tev:PullMessagesResponse xmlns:tev="http://www.onvif.org/ver10/events/wsdl" xmlns:wsnt="http://docs.oasis-open.org/wsn/b-2">
                      <wsnt:NotificationMessage>
                        <wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:VideoSource/SignalLoss</wsnt:Topic>
                      </wsnt:NotificationMessage>
                    </tev:PullMessagesResponse>
                    """);
            }
            else
            {
                cevap = SoapSar("<tds:UnknownResponse xmlns:tds=\"http://www.onvif.org/ver10/device/wsdl\"/>");
            }

            byte[] tampon = Encoding.UTF8.GetBytes(cevap);
            ctx.Response.ContentType = "application/soap+xml; charset=utf-8";
            ctx.Response.ContentLength64 = tampon.Length;
            await ctx.Response.OutputStream.WriteAsync(tampon).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Sahte ONVIF istek işleme hatası");
        }
        finally
        {
            ctx.Response.Close();
        }
    }

    private static string SoapSar(string govde) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope">
          <s:Body>{govde}</s:Body>
        </s:Envelope>
        """;

    public void Dispose()
    {
        try
        {
            _iptal?.Cancel();
            _dinleyici.Stop();
            _dinleyici.Close();
        }
        catch
        {
            // görmezden gel
        }
    }
}
