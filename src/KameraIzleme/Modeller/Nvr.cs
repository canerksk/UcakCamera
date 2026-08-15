namespace KameraIzleme.Modeller;

/// <summary>
/// Kayıt cihazı (NVR). Kameralar isteğe bağlı olarak bir NVR'a bağlanabilir.
/// </summary>
public sealed class Nvr
{
    public long Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public string? Kullanici { get; set; }
    public string? SifreSifreli { get; set; }
    public string Saglayici { get; set; } = "evrensel";
    public bool Aktif { get; set; } = true;
}
