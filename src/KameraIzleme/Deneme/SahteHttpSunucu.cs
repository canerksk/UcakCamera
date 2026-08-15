using System.Net;
using System.Text;
using Serilog;

namespace KameraIzleme.Deneme;

/// <summary>
/// Küçük bir yerel HTTP sunucusu. Deneme modunda Hikvision benzeri bir cihazı taklit eder;
/// hem <see cref="Saglayicilar.TanimliSaglayici"/> hem de <see cref="Saglayicilar.EvrenselSaglayici"/>
/// (ping başarılı, RTSP OPTIONS için ayrı sınıf) katmanları test edilir.
/// </summary>
public sealed class SahteHttpSunucu : IDisposable
{
    private readonly HttpListener _dinleyici = new();
    private CancellationTokenSource? _iptal;
    private Task? _dongu;

    public string OnEk { get; }

    public SahteHttpSunucu(int port = 18080)
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
            Log.Warning(ex, "Sahte HTTP sunucusu başlatılamadı — deneme modu HTTP kısmı devre dışı");
            return;
        }

        _iptal = new CancellationTokenSource();
        _dongu = Task.Run(() => DinleAsync(_iptal.Token));
        Log.Information("Sahte HTTP sunucusu ayakta: {U}", OnEk);
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
            catch (Exception)
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
            string url = ctx.Request.Url?.AbsolutePath ?? "/";
            (int kod, string tur, string govde) = url switch
            {
                "/ISAPI/System/status" => (200, "application/xml",
                    "<DeviceStatus><currentDeviceTime>2025-01-01T00:00:00</currentDeviceTime></DeviceStatus>"),
                "/doc/page/login.asp" => (200, "text/html", "<html><body>SahteHik</body></html>"),
                "/ISAPI/ContentMgmt/InputProxy/channels" => (200, "application/xml",
                    "<InputProxyChannelList>" +
                    "<InputProxyChannel><id>1</id><name>Kanal 1</name>" +
                    "<sourceInputPortDescriptor><ipAddress>192.0.2.10</ipAddress></sourceInputPortDescriptor>" +
                    "<online>true</online></InputProxyChannel>" +
                    "</InputProxyChannelList>"),
                _ => (404, "text/plain", "yok"),
            };

            ctx.Response.StatusCode = kod;
            ctx.Response.ContentType = tur;
            byte[] tampon = Encoding.UTF8.GetBytes(govde);
            ctx.Response.ContentLength64 = tampon.Length;
            await ctx.Response.OutputStream.WriteAsync(tampon).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Sahte HTTP istek işleme hatası");
        }
        finally
        {
            ctx.Response.Close();
        }
    }

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
