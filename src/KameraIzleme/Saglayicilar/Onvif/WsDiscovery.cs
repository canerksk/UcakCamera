using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using Serilog;

namespace KameraIzleme.Saglayicilar.Onvif;

/// <summary>
/// WS-Discovery ile ağdaki ONVIF cihazlarını multicast SOAP mesajıyla arar.
/// UDP 239.255.255.250:3702.
/// </summary>
public sealed class WsDiscovery
{
    private static readonly IPEndPoint Multicast = new(IPAddress.Parse("239.255.255.250"), 3702);

    public async Task<IReadOnlyList<KesfedilenCihaz>> KesifYapAsync(TimeSpan bekleme, CancellationToken ct)
    {
        var sonuclar = new Dictionary<string, KesfedilenCihaz>(StringComparer.OrdinalIgnoreCase);
        string messageId = "uuid:" + Guid.NewGuid().ToString();

        string probe = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="{OnvifSoap.NsSoap}"
                        xmlns:a="http://schemas.xmlsoap.org/ws/2004/08/addressing"
                        xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery">
              <s:Header>
                <a:Action s:mustUnderstand="1">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action>
                <a:MessageID>{messageId}</a:MessageID>
                <a:To s:mustUnderstand="1">urn:schemas-xmlsoap-org:ws:2005:04:discovery</a:To>
              </s:Header>
              <s:Body>
                <d:Probe>
                  <d:Types xmlns:dp0="http://www.onvif.org/ver10/network/wsdl">dp0:NetworkVideoTransmitter</d:Types>
                </d:Probe>
              </s:Body>
            </s:Envelope>
            """;

        byte[] paket = Encoding.UTF8.GetBytes(probe);

        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.EnableBroadcast = true;
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

        try
        {
            await udp.SendAsync(paket, paket.Length, Multicast).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "WS-Discovery paketi gönderilemedi");
            return Array.Empty<KesfedilenCihaz>();
        }

        using var iptalKaynagi = CancellationTokenSource.CreateLinkedTokenSource(ct);
        iptalKaynagi.CancelAfter(bekleme);

        while (!iptalKaynagi.Token.IsCancellationRequested)
        {
            UdpReceiveResult sonuc;
            try
            {
                sonuc = await udp.ReceiveAsync(iptalKaynagi.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "WS-Discovery alma hatası");
                break;
            }

            try
            {
                string cevap = Encoding.UTF8.GetString(sonuc.Buffer);
                var belge = XDocument.Parse(cevap);
                XNamespace d = "http://schemas.xmlsoap.org/ws/2005/04/discovery";
                foreach (var pm in belge.Descendants(d + "ProbeMatch"))
                {
                    string? xaddrs = pm.Element(d + "XAddrs")?.Value;
                    if (string.IsNullOrWhiteSpace(xaddrs))
                    {
                        continue;
                    }

                    foreach (var xaddr in xaddrs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!Uri.TryCreate(xaddr, UriKind.Absolute, out var u))
                        {
                            continue;
                        }

                        string anahtar = u.Host + ":" + u.Port;
                        if (sonuclar.ContainsKey(anahtar))
                        {
                            continue;
                        }

                        sonuclar[anahtar] = new KesfedilenCihaz
                        {
                            Adres = u.Host,
                            Port = u.Port,
                            OnvifServisUrl = xaddr,
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "WS-Discovery ayrıştırma hatası");
            }
        }

        return sonuclar.Values.ToList();
    }
}

public sealed class KesfedilenCihaz
{
    public string Adres { get; set; } = string.Empty;
    public int Port { get; set; }
    public string OnvifServisUrl { get; set; } = string.Empty;
    public string? Uretici { get; set; }
    public string? Model { get; set; }
    public string? Firmware { get; set; }
    public string? SeriNo { get; set; }
    public string? RtspUrl { get; set; }
}
