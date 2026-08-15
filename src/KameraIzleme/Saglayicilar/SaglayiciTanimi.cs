using System.Text.Json.Serialization;

namespace KameraIzleme.Saglayicilar;

/// <summary>
/// JSON tanım dosyasının şeması. Bir tanım dosyası bir markanın parmak izini,
/// kimlik doğrulama biçimini, RTSP şablonlarını, durum uçlarını ve olay akışını tarif eder.
/// </summary>
public sealed class SaglayiciTanimi
{
    [JsonPropertyName("marka")]
    public string Marka { get; set; } = string.Empty;

    [JsonPropertyName("oncelik")]
    public int Oncelik { get; set; } = 100;

    [JsonPropertyName("parmakizi")]
    public ParmakIzi Parmakizi { get; set; } = new();

    [JsonPropertyName("kimlik_dogrulama")]
    public string KimlikDogrulama { get; set; } = "digest"; // digest | basic | yok

    [JsonPropertyName("rtsp")]
    public RtspSablonlari? Rtsp { get; set; }

    [JsonPropertyName("durum_kontrolu")]
    public UcTanimi? DurumKontrolu { get; set; }

    [JsonPropertyName("kanal_listesi")]
    public UcTanimi? KanalListesi { get; set; }

    [JsonPropertyName("olay_akisi")]
    public OlayAkisTanimi? OlayAkisi { get; set; }
}

public sealed class ParmakIzi
{
    [JsonPropertyName("onvif_uretici_icerir")]
    public List<string> OnvifUreticiIcerir { get; set; } = new();

    [JsonPropertyName("http_server_basligi_icerir")]
    public List<string> HttpServerBasligiIcerir { get; set; } = new();

    [JsonPropertyName("http_yol_kontrolu")]
    public List<HttpYolKontrolu> HttpYolKontrolu { get; set; } = new();
}

public sealed class HttpYolKontrolu
{
    [JsonPropertyName("yol")]
    public string Yol { get; set; } = "/";

    [JsonPropertyName("beklenen_durum")]
    public int BeklenenDurum { get; set; } = 200;
}

public sealed class RtspSablonlari
{
    [JsonPropertyName("ana_akis")]
    public string? AnaAkis { get; set; }

    [JsonPropertyName("alt_akis")]
    public string? AltAkis { get; set; }
}

public sealed class UcTanimi
{
    [JsonPropertyName("yol")]
    public string Yol { get; set; } = "/";

    [JsonPropertyName("format")]
    public string Format { get; set; } = "xml"; // xml | json | duzmetin

    [JsonPropertyName("basari_kosulu")]
    public string? BasariKosulu { get; set; } // XPath / JSONPath / regex

    [JsonPropertyName("liste_yolu")]
    public string? ListeYolu { get; set; }

    [JsonPropertyName("alan_eslemesi")]
    public Dictionary<string, string> AlanEslemesi { get; set; } = new();
}

public sealed class OlayAkisTanimi
{
    [JsonPropertyName("yol")]
    public string Yol { get; set; } = "/";

    [JsonPropertyName("tip")]
    public string Tip { get; set; } = "multipart"; // multipart | jsonstream | duzmetin

    [JsonPropertyName("format")]
    public string Format { get; set; } = "xml"; // xml | json | duzmetin

    [JsonPropertyName("olay_yolu")]
    public string? OlayYolu { get; set; }

    [JsonPropertyName("alan_eslemesi")]
    public Dictionary<string, string> AlanEslemesi { get; set; } = new();

    [JsonPropertyName("olay_eslemesi")]
    public Dictionary<string, string> OlayEslemesi { get; set; } = new();
}
