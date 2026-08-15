namespace KameraIzleme.Modeller;

/// <summary>
/// Bir cihazın sağlayıcı seçimine yetecek özet bilgisi (ONVIF ya da HTTP parmak izi taramasından toplanır).
/// </summary>
public sealed class CihazBilgisi
{
    public string Ip { get; set; } = string.Empty;
    public int HttpPort { get; set; } = 80;
    public int OnvifPort { get; set; } = 80;
    public string? OnvifUretici { get; set; }
    public string? OnvifModel { get; set; }
    public string? OnvifSeriNo { get; set; }
    public string? HttpServerBasligi { get; set; }
    public IReadOnlyDictionary<string, int> HttpYolDurumlari { get; set; } =
        new Dictionary<string, int>();
    public string? Kullanici { get; set; }
    public string? Sifre { get; set; }
}

/// <summary>Bir kontrol turunun özet sonucu.</summary>
public sealed class KontrolSonucu
{
    public bool Basarili { get; set; }
    public string AktifKatman { get; set; } = "evrensel";
    public int? GecikmeMs { get; set; }
    public string? Mesaj { get; set; }
    public string? OlayTipi { get; set; }
}

/// <summary>NVR üzerinden gelen bir kanalın özeti.</summary>
public sealed class KanalBilgisi
{
    public string KanalNo { get; set; } = string.Empty;
    public string? Ad { get; set; }
    public string? Ip { get; set; }
    public bool Cevrimici { get; set; }
}

/// <summary>Cihazdan asenkron akan bir olay.</summary>
public sealed class CihazOlayi
{
    public required string Tip { get; init; }
    public string? Mesaj { get; init; }
    public string? KanalNo { get; init; }
    public DateTime Zaman { get; init; } = DateTime.UtcNow;
}
