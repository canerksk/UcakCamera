using Dapper;
using KameraIzleme.Modeller;

namespace KameraIzleme.Veri;

public sealed class OlayDeposu
{
    public long AcikOlayAc(KameraOlayi olay)
    {
        using var b = VeriTabani.Ac();
        return b.ExecuteScalar<long>(
            """
            INSERT INTO kamera_olaylari (kamera_id, tip, kaynak, mesaj, basladi, mail_gonderildi)
            VALUES (@KameraId, @Tip, @Kaynak, @Mesaj, @Basladi, 0);
            SELECT last_insert_rowid();
            """, olay);
    }

    /// <summary>Belirli kamera + tip için hâlâ açık bir olay varsa döner.</summary>
    public KameraOlayi? AcikOlay(long kameraId, string? tip = null)
    {
        using var b = VeriTabani.Ac();
        string sql = SqlSecim + " WHERE kamera_id=@K AND bitti IS NULL";
        if (tip != null)
        {
            sql += " AND tip=@T";
        }

        sql += " ORDER BY basladi DESC LIMIT 1";
        return b.QuerySingleOrDefault<KameraOlayi>(sql, new { K = kameraId, T = tip });
    }

    public void OlayKapat(long id, DateTime bittiUtc)
    {
        using var b = VeriTabani.Ac();
        b.Execute(
            """
            UPDATE kamera_olaylari
            SET bitti=@Bitti,
                sure_saniye=CAST((julianday(@Bitti) - julianday(basladi)) * 86400 AS INTEGER)
            WHERE id=@Id
            """, new { Id = id, Bitti = bittiUtc });
    }

    public void MailGonderildiIsaretle(long olayId)
    {
        using var b = VeriTabani.Ac();
        b.Execute(
            "UPDATE kamera_olaylari SET mail_gonderildi=1 WHERE id=@Id",
            new { Id = olayId });
    }

    /// <summary>Filtrelenmiş olay listesi.</summary>
    public IReadOnlyList<KameraOlayi> Filtrele(
        DateTime? baslangic = null,
        DateTime? bitis = null,
        long? kameraId = null,
        string? tip = null,
        string? kaynak = null,
        int limit = 5000)
    {
        var kosullar = new List<string>();
        var parametreler = new DynamicParameters();
        if (baslangic is not null) { kosullar.Add("basladi >= @B"); parametreler.Add("B", baslangic); }
        if (bitis is not null) { kosullar.Add("basladi <= @E"); parametreler.Add("E", bitis); }
        if (kameraId is not null) { kosullar.Add("kamera_id = @K"); parametreler.Add("K", kameraId); }
        if (!string.IsNullOrEmpty(tip)) { kosullar.Add("tip = @T"); parametreler.Add("T", tip); }
        if (!string.IsNullOrEmpty(kaynak)) { kosullar.Add("kaynak = @Ka"); parametreler.Add("Ka", kaynak); }
        parametreler.Add("L", limit);

        string where = kosullar.Count > 0 ? " WHERE " + string.Join(" AND ", kosullar) : string.Empty;
        string sql = SqlSecim + where + " ORDER BY basladi DESC LIMIT @L";

        using var b = VeriTabani.Ac();
        return b.Query<KameraOlayi>(sql, parametreler).AsList();
    }

    /// <summary>Verilen kamera için verilen pencerede toplam kesinti (saniye).</summary>
    public long KesintiSuresiSaniye(long kameraId, DateTime baslangic, DateTime bitis)
    {
        using var b = VeriTabani.Ac();
        return b.ExecuteScalar<long?>(
            """
            SELECT COALESCE(SUM(
                CAST((julianday(COALESCE(bitti, @E)) - julianday(MAX(basladi, @B))) * 86400 AS INTEGER)
            ), 0)
            FROM kamera_olaylari
            WHERE kamera_id=@K
              AND tip IN ('dustu','dondu','sinyal_kaybi','donanim_hatasi')
              AND basladi <= @E
              AND (bitti IS NULL OR bitti >= @B)
            """, new { K = kameraId, B = baslangic, E = bitis }) ?? 0;
    }

    /// <summary>Son 30 gündeki en sık düşen kameraların id + sayı listesi.</summary>
    public IReadOnlyList<(long KameraId, int Sayi)> EnSikDusenler(int gunSayisi = 30, int limit = 10)
    {
        using var b = VeriTabani.Ac();
        DateTime baslangic = DateTime.UtcNow.AddDays(-gunSayisi);
        return b.Query<(long KameraId, int Sayi)>(
            """
            SELECT kamera_id AS KameraId, COUNT(*) AS Sayi
            FROM kamera_olaylari
            WHERE basladi>=@B AND tip IN ('dustu','dondu','sinyal_kaybi')
            GROUP BY kamera_id
            ORDER BY Sayi DESC
            LIMIT @L
            """, new { B = baslangic, L = limit }).AsList();
    }

    private const string SqlSecim = """
        SELECT id AS Id, kamera_id AS KameraId, tip AS Tip, kaynak AS Kaynak, mesaj AS Mesaj,
               basladi AS Basladi, bitti AS Bitti, sure_saniye AS SureSaniye,
               mail_gonderildi AS MailGonderildi
        FROM kamera_olaylari
        """;
}
