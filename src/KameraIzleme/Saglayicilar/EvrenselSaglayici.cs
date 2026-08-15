using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using KameraIzleme.Modeller;
using KameraIzleme.Veri;
using Serilog;

namespace KameraIzleme.Saglayicilar;

/// <summary>
/// Katman 1: her cihazda çalışan minimum izleme.
/// 1) ICMP ping, 2) ping başarılıysa RTSP OPTIONS.
/// Diğer katmanların hepsi başarısız olsa bile bu katman izlemeye devam eder.
/// </summary>
public sealed class EvrenselSaglayici : IKameraSaglayici
{
    private readonly AyarlarDeposu _ayarlar;

    public EvrenselSaglayici(AyarlarDeposu ayarlar) => _ayarlar = ayarlar;

    public string Marka => "evrensel";

    public int Oncelik => 0;

    public Task<bool> DesteklerMiAsync(CihazBilgisi cihaz, CancellationToken ct) =>
        Task.FromResult(true); // Her cihazda çalışır.

    public async Task<KontrolSonucu> DurumKontrolAsync(Kamera kamera, CancellationToken ct)
    {
        int pingTimeout = _ayarlar.Al("izleme.ping_timeout_ms", 2000);
        int rtspTimeout = _ayarlar.Al("izleme.rtsp_timeout_ms", 3000);

        try
        {
            using var ping = new Ping();
            var yanit = await ping.SendPingAsync(kamera.Ip, pingTimeout).ConfigureAwait(false);

            if (yanit.Status != IPStatus.Success)
            {
                return new KontrolSonucu
                {
                    Basarili = false,
                    AktifKatman = "evrensel",
                    Mesaj = $"Ping başarısız: {yanit.Status}",
                    OlayTipi = OlayTipleri.Dustu,
                };
            }

            int gecikme = (int)yanit.RoundtripTime;

            // Ping geçti — RTSP OPTIONS ile servisi doğrula.
            var rtspSonuc = await RtspOptionsAsync(kamera, rtspTimeout, ct).ConfigureAwait(false);
            if (rtspSonuc is { } hata)
            {
                return new KontrolSonucu
                {
                    Basarili = false,
                    AktifKatman = "evrensel",
                    GecikmeMs = gecikme,
                    Mesaj = hata,
                    OlayTipi = OlayTipleri.Dondu,
                };
            }

            return new KontrolSonucu
            {
                Basarili = true,
                AktifKatman = "evrensel",
                GecikmeMs = gecikme,
                Mesaj = "Ping ve RTSP başarılı",
            };
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Evrensel katman kontrolü hata verdi: {Ip}", kamera.Ip);
            return new KontrolSonucu
            {
                Basarili = false,
                AktifKatman = "evrensel",
                Mesaj = $"Kontrol hatası: {ex.Message}",
                OlayTipi = OlayTipleri.Dustu,
            };
        }
    }

    public Task<IReadOnlyList<KanalBilgisi>> KanallariGetirAsync(Kamera kamera, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<KanalBilgisi>>(Array.Empty<KanalBilgisi>());

    public async IAsyncEnumerable<CihazOlayi> OlaylariDinleAsync(
        Kamera kamera,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>RTSP OPTIONS ile servisin ayakta olup olmadığını sınar.</summary>
    private static async Task<string?> RtspOptionsAsync(Kamera kamera, int timeoutMs, CancellationToken ct)
    {
        string yol = string.IsNullOrEmpty(kamera.RtspAnaAkis) ? "/" : kamera.RtspAnaAkis;
        string url = $"rtsp://{kamera.Ip}:{kamera.RtspPort}{yol}";
        string istek =
            $"OPTIONS {url} RTSP/1.0\r\n" +
            "CSeq: 1\r\n" +
            "User-Agent: KameraIzleme\r\n" +
            "\r\n";

        try
        {
            using var istemci = new TcpClient();
            var baglantiTask = istemci.ConnectAsync(kamera.Ip, kamera.RtspPort);
            var tamamlanan = await Task.WhenAny(baglantiTask, Task.Delay(timeoutMs, ct)).ConfigureAwait(false);
            if (tamamlanan != baglantiTask)
            {
                return "RTSP bağlantı zaman aşımı";
            }

            await baglantiTask.ConfigureAwait(false);

            var stopwatch = Stopwatch.StartNew();
            using var akis = istemci.GetStream();
            akis.ReadTimeout = timeoutMs;
            akis.WriteTimeout = timeoutMs;

            byte[] tampon = Encoding.ASCII.GetBytes(istek);
            await akis.WriteAsync(tampon, ct).ConfigureAwait(false);

            byte[] buf = new byte[1024];
            using var bekleme = new CancellationTokenSource(timeoutMs);
            using var birlesik = CancellationTokenSource.CreateLinkedTokenSource(ct, bekleme.Token);
            int okunan = await akis.ReadAsync(buf, birlesik.Token).ConfigureAwait(false);
            stopwatch.Stop();

            if (okunan <= 0)
            {
                return "RTSP: cevap alınamadı";
            }

            string cevap = Encoding.ASCII.GetString(buf, 0, okunan);
            // 200 OK ya da 401 Unauthorized her ikisi de servisin ayakta olduğunu gösterir.
            if (cevap.StartsWith("RTSP/1.0 200", StringComparison.Ordinal) ||
                cevap.StartsWith("RTSP/1.0 401", StringComparison.Ordinal))
            {
                return null;
            }

            return $"RTSP beklenmeyen yanıt: {cevap.Split('\n')[0].Trim()}";
        }
        catch (SocketException se)
        {
            return $"RTSP soket hatası: {se.SocketErrorCode}";
        }
        catch (IOException io)
        {
            return $"RTSP okuma hatası: {io.Message}";
        }
        catch (OperationCanceledException)
        {
            return "RTSP zaman aşımı";
        }
    }
}
