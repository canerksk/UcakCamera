using Dapper;
using KameraIzleme.Modeller;

namespace KameraIzleme.Veri;

/// <summary>Kamera CRUD deposu. Dapper üzerinde ince bir sarmalayıcı.</summary>
public sealed class KameraDeposu
{
    public IReadOnlyList<Kamera> Tumu()
    {
        using var b = VeriTabani.Ac();
        return b.Query<Kamera>(SqlSecim + " ORDER BY ad").AsList();
    }

    public Kamera? Getir(long id)
    {
        using var b = VeriTabani.Ac();
        return b.QuerySingleOrDefault<Kamera>(SqlSecim + " WHERE id=@Id", new { Id = id });
    }

    public Kamera? IpIleGetir(string ip)
    {
        using var b = VeriTabani.Ac();
        return b.QuerySingleOrDefault<Kamera>(SqlSecim + " WHERE ip=@Ip", new { Ip = ip });
    }

    public long Ekle(Kamera k)
    {
        k.Olusturma = DateTime.UtcNow;
        k.Guncelleme = DateTime.UtcNow;
        using var b = VeriTabani.Ac();
        return b.ExecuteScalar<long>(
            """
            INSERT INTO kameralar
                (ad, ip, rtsp_port, rtsp_ana_akis, rtsp_alt_akis, http_port, onvif_port,
                 kullanici, sifre_sifreli, marka, model, seri_no, firmware,
                 saglayici, saglayici_elle_secildi, lokasyon, nvr_id, aktif, olusturma, guncelleme)
            VALUES
                (@Ad, @Ip, @RtspPort, @RtspAnaAkis, @RtspAltAkis, @HttpPort, @OnvifPort,
                 @Kullanici, @SifreSifreli, @Marka, @Model, @SeriNo, @Firmware,
                 @Saglayici, @SaglayiciElleSecildi, @Lokasyon, @NvrId, @Aktif, @Olusturma, @Guncelleme);
            SELECT last_insert_rowid();
            """, k);
    }

    public void Guncelle(Kamera k)
    {
        k.Guncelleme = DateTime.UtcNow;
        using var b = VeriTabani.Ac();
        b.Execute(
            """
            UPDATE kameralar SET
                ad=@Ad, ip=@Ip, rtsp_port=@RtspPort, rtsp_ana_akis=@RtspAnaAkis,
                rtsp_alt_akis=@RtspAltAkis, http_port=@HttpPort, onvif_port=@OnvifPort,
                kullanici=@Kullanici, sifre_sifreli=@SifreSifreli,
                marka=@Marka, model=@Model, seri_no=@SeriNo, firmware=@Firmware,
                saglayici=@Saglayici, saglayici_elle_secildi=@SaglayiciElleSecildi,
                lokasyon=@Lokasyon, nvr_id=@NvrId, aktif=@Aktif, guncelleme=@Guncelleme
            WHERE id=@Id
            """, k);
    }

    public void Sil(long id)
    {
        using var b = VeriTabani.Ac();
        b.Execute("DELETE FROM kameralar WHERE id=@Id", new { Id = id });
    }

    private const string SqlSecim = """
        SELECT id AS Id, ad AS Ad, ip AS Ip, rtsp_port AS RtspPort,
               rtsp_ana_akis AS RtspAnaAkis, rtsp_alt_akis AS RtspAltAkis,
               http_port AS HttpPort, onvif_port AS OnvifPort,
               kullanici AS Kullanici, sifre_sifreli AS SifreSifreli,
               marka AS Marka, model AS Model, seri_no AS SeriNo, firmware AS Firmware,
               saglayici AS Saglayici, saglayici_elle_secildi AS SaglayiciElleSecildi,
               lokasyon AS Lokasyon, nvr_id AS NvrId, aktif AS Aktif,
               olusturma AS Olusturma, guncelleme AS Guncelleme
        FROM kameralar
        """;
}
