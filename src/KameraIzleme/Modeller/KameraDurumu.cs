namespace KameraIzleme.Modeller;

/// <summary>
/// Bir kameranın anlık durumu — sadece son durumu tutar, üzerine yazılır.
/// Geçmiş için <see cref="KameraOlayi"/> kullanılır.
/// </summary>
public sealed class KameraDurumu
{
    public long KameraId { get; set; }
    public bool Cevrimici { get; set; }
    public int ArdisikHata { get; set; }
    public int? GecikmeMs { get; set; }
    public string AktifKatman { get; set; } = "evrensel";
    public DateTime SonKontrol { get; set; }
    public DateTime? SonBasariliKontrol { get; set; }
    public string? SonMesaj { get; set; }
}
