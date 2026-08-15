namespace KameraIzleme.Modeller;

/// <summary>
/// İzlenen tek bir IP kamera kaydı. Tablodaki alan adlarıyla birebir eşleşir.
/// </summary>
public sealed class Kamera
{
    public long Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int RtspPort { get; set; } = 554;
    public string? RtspAnaAkis { get; set; }
    public string? RtspAltAkis { get; set; }
    public int HttpPort { get; set; } = 80;
    public int OnvifPort { get; set; } = 80;
    public string? Kullanici { get; set; }
    public string? SifreSifreli { get; set; }
    public string? Marka { get; set; }
    public string? Model { get; set; }
    public string? SeriNo { get; set; }
    public string? Firmware { get; set; }
    public string Saglayici { get; set; } = "evrensel";
    public bool SaglayiciElleSecildi { get; set; }
    public string? Lokasyon { get; set; }
    public long? NvrId { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime Olusturma { get; set; } = DateTime.UtcNow;
    public DateTime Guncelleme { get; set; } = DateTime.UtcNow;
}
