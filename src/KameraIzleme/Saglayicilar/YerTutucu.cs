using System.Text.RegularExpressions;
using KameraIzleme.Modeller;

namespace KameraIzleme.Saglayicilar;

/// <summary>Şablon içindeki <c>{ip}</c>, <c>{kullanici}</c>, <c>{kanal}</c> gibi
/// yer tutucuları verilen sözlükten çözer.</summary>
public static class YerTutucu
{
    private static readonly Regex YerTutucuRegex = new(@"\{(?<ad>[a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

    public static string Coz(string sablon, IReadOnlyDictionary<string, string?> degerler)
    {
        return YerTutucuRegex.Replace(sablon, m =>
        {
            string ad = m.Groups["ad"].Value;
            return degerler.TryGetValue(ad, out var d) && d is not null ? d : m.Value;
        });
    }

    /// <summary>Kamera için standart değer sözlüğü.</summary>
    public static IReadOnlyDictionary<string, string?> KameraSozlugu(Kamera k, string? kanalNo = null)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ip"] = k.Ip,
            ["kullanici"] = k.Kullanici,
            ["kanal"] = kanalNo ?? "1",
            ["rtsp_port"] = k.RtspPort.ToString(),
            ["http_port"] = k.HttpPort.ToString(),
            ["onvif_port"] = k.OnvifPort.ToString(),
        };
    }
}
