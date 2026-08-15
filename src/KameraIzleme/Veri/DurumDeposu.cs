using Dapper;
using KameraIzleme.Modeller;

namespace KameraIzleme.Veri;

/// <summary>
/// Anlık durum tablosu. UPSERT ile idempotent, üzerine yazar.
/// </summary>
public sealed class DurumDeposu
{
    public KameraDurumu? Getir(long kameraId)
    {
        using var b = VeriTabani.Ac();
        return b.QuerySingleOrDefault<KameraDurumu>(
            SqlSecim + " WHERE kamera_id=@Id", new { Id = kameraId });
    }

    public IReadOnlyList<KameraDurumu> Tumu()
    {
        using var b = VeriTabani.Ac();
        return b.Query<KameraDurumu>(SqlSecim).AsList();
    }

    public void KayitEt(KameraDurumu d)
    {
        using var b = VeriTabani.Ac();
        b.Execute(
            """
            INSERT INTO kamera_durumlari
                (kamera_id, cevrimici, ardisik_hata, gecikme_ms, aktif_katman,
                 son_kontrol, son_basarili_kontrol, son_mesaj)
            VALUES
                (@KameraId, @Cevrimici, @ArdisikHata, @GecikmeMs, @AktifKatman,
                 @SonKontrol, @SonBasariliKontrol, @SonMesaj)
            ON CONFLICT(kamera_id) DO UPDATE SET
                cevrimici            = excluded.cevrimici,
                ardisik_hata         = excluded.ardisik_hata,
                gecikme_ms           = excluded.gecikme_ms,
                aktif_katman         = excluded.aktif_katman,
                son_kontrol          = excluded.son_kontrol,
                son_basarili_kontrol = excluded.son_basarili_kontrol,
                son_mesaj            = excluded.son_mesaj
            """, d);
    }

    public void GecikmeEkle(long kameraId, int gecikmeMs)
    {
        using var b = VeriTabani.Ac();
        b.Execute(
            "INSERT INTO gecikme_gecmisi (kamera_id, zaman, gecikme_ms) VALUES (@K, @Z, @G)",
            new { K = kameraId, Z = DateTime.UtcNow, G = gecikmeMs });
    }

    public IReadOnlyList<(DateTime Zaman, int GecikmeMs)> GecikmeGetir(long kameraId, DateTime enErkeni)
    {
        using var b = VeriTabani.Ac();
        return b.Query<(DateTime Zaman, int GecikmeMs)>(
            """
            SELECT zaman AS Zaman, gecikme_ms AS GecikmeMs
            FROM gecikme_gecmisi
            WHERE kamera_id=@K AND zaman>=@Z
            ORDER BY zaman
            """, new { K = kameraId, Z = enErkeni }).AsList();
    }

    /// <summary>Verilen tarihten eski gecikme kayıtlarını temizler.</summary>
    public void EskiGecikmeleriTemizle(DateTime enErkeni)
    {
        using var b = VeriTabani.Ac();
        b.Execute("DELETE FROM gecikme_gecmisi WHERE zaman<@Z", new { Z = enErkeni });
    }

    private const string SqlSecim = """
        SELECT kamera_id AS KameraId, cevrimici AS Cevrimici, ardisik_hata AS ArdisikHata,
               gecikme_ms AS GecikmeMs, aktif_katman AS AktifKatman,
               son_kontrol AS SonKontrol, son_basarili_kontrol AS SonBasariliKontrol,
               son_mesaj AS SonMesaj
        FROM kamera_durumlari
        """;
}
