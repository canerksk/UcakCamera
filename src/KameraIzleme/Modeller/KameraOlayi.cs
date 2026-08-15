namespace KameraIzleme.Modeller;

/// <summary>
/// Uptime hesabı ve geçmiş için tutulan olay kaydı. Sadece durum değiştiğinde satır eklenir.
/// </summary>
public sealed class KameraOlayi
{
    public long Id { get; set; }
    public long KameraId { get; set; }
    public string Tip { get; set; } = string.Empty;
    public string Kaynak { get; set; } = "evrensel";
    public string? Mesaj { get; set; }
    public DateTime Basladi { get; set; }
    public DateTime? Bitti { get; set; }
    public long? SureSaniye { get; set; }
    public bool MailGonderildi { get; set; }
}

/// <summary>Bilinen olay tipleri.</summary>
public static class OlayTipleri
{
    public const string Dustu = "dustu";
    public const string Dondu = "dondu";
    public const string SinyalKaybi = "sinyal_kaybi";
    public const string DonanimHatasi = "donanim_hatasi";
    public const string YetkisizErisim = "yetkisiz_erisim";
    public const string DiskHatasi = "disk_hatasi";
}
