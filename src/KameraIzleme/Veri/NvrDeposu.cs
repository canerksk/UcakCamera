using Dapper;
using KameraIzleme.Modeller;

namespace KameraIzleme.Veri;

public sealed class NvrDeposu
{
    public IReadOnlyList<Nvr> Tumu()
    {
        using var b = VeriTabani.Ac();
        return b.Query<Nvr>(SqlSecim + " ORDER BY ad").AsList();
    }

    public Nvr? Getir(long id)
    {
        using var b = VeriTabani.Ac();
        return b.QuerySingleOrDefault<Nvr>(SqlSecim + " WHERE id=@Id", new { Id = id });
    }

    public long Ekle(Nvr n)
    {
        using var b = VeriTabani.Ac();
        return b.ExecuteScalar<long>(
            """
            INSERT INTO nvrlar (ad, ip, marka, model, kullanici, sifre_sifreli, saglayici, aktif)
            VALUES (@Ad, @Ip, @Marka, @Model, @Kullanici, @SifreSifreli, @Saglayici, @Aktif);
            SELECT last_insert_rowid();
            """, n);
    }

    public void Guncelle(Nvr n)
    {
        using var b = VeriTabani.Ac();
        b.Execute(
            """
            UPDATE nvrlar SET
                ad=@Ad, ip=@Ip, marka=@Marka, model=@Model,
                kullanici=@Kullanici, sifre_sifreli=@SifreSifreli,
                saglayici=@Saglayici, aktif=@Aktif
            WHERE id=@Id
            """, n);
    }

    public void Sil(long id)
    {
        using var b = VeriTabani.Ac();
        b.Execute("DELETE FROM nvrlar WHERE id=@Id", new { Id = id });
    }

    private const string SqlSecim = """
        SELECT id AS Id, ad AS Ad, ip AS Ip, marka AS Marka, model AS Model,
               kullanici AS Kullanici, sifre_sifreli AS SifreSifreli,
               saglayici AS Saglayici, aktif AS Aktif
        FROM nvrlar
        """;
}
