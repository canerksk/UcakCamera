using Microsoft.Data.Sqlite;

namespace KameraIzleme.Veri;

/// <summary>
/// SQLite bağlantısı ve şema kurulumu. Exe'nin yanında <c>kamera.db</c>
/// dosyasını tutar; ilk açılışta şemayı kurar.
/// </summary>
public static class VeriTabani
{
    private static readonly object KilitNesnesi = new();
    private static string? _baglantiDizesi;

    /// <summary>Uygulama boyunca kullanılacak bağlantı dizesini döner.</summary>
    public static string BaglantiDizesi
    {
        get
        {
            if (_baglantiDizesi != null)
            {
                return _baglantiDizesi;
            }

            lock (KilitNesnesi)
            {
                if (_baglantiDizesi == null)
                {
                    string dizin = AppContext.BaseDirectory;
                    string dosya = Path.Combine(dizin, "kamera.db");
                    _baglantiDizesi = new SqliteConnectionStringBuilder
                    {
                        DataSource = dosya,
                        Mode = SqliteOpenMode.ReadWriteCreate,
                        Cache = SqliteCacheMode.Shared,
                    }.ToString();
                }
            }

            return _baglantiDizesi;
        }
    }

    /// <summary>Yeni bir açık bağlantı üretir. Kullanan taraf using ile yönetir.</summary>
    public static SqliteConnection Ac()
    {
        var baglanti = new SqliteConnection(BaglantiDizesi);
        baglanti.Open();

        // WAL modu okuma/yazma çakışmasını azaltır.
        using var pragma = baglanti.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();

        return baglanti;
    }

    /// <summary>Şemayı kurar. Idempotent — tekrar çağrılabilir.</summary>
    public static void SemayiKur()
    {
        using var baglanti = Ac();
        using var komut = baglanti.CreateCommand();
        komut.CommandText = Sema;
        komut.ExecuteNonQuery();
    }

    private const string Sema = """
        CREATE TABLE IF NOT EXISTS kameralar (
            id                     INTEGER PRIMARY KEY AUTOINCREMENT,
            ad                     TEXT    NOT NULL,
            ip                     TEXT    NOT NULL UNIQUE,
            rtsp_port              INTEGER NOT NULL DEFAULT 554,
            rtsp_ana_akis          TEXT,
            rtsp_alt_akis          TEXT,
            http_port              INTEGER NOT NULL DEFAULT 80,
            onvif_port             INTEGER NOT NULL DEFAULT 80,
            kullanici              TEXT,
            sifre_sifreli          TEXT,
            marka                  TEXT,
            model                  TEXT,
            seri_no                TEXT,
            firmware               TEXT,
            saglayici              TEXT    NOT NULL DEFAULT 'evrensel',
            saglayici_elle_secildi INTEGER NOT NULL DEFAULT 0,
            lokasyon               TEXT,
            nvr_id                 INTEGER,
            aktif                  INTEGER NOT NULL DEFAULT 1,
            olusturma              TEXT    NOT NULL,
            guncelleme             TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS nvrlar (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            ad            TEXT    NOT NULL,
            ip            TEXT    NOT NULL,
            marka         TEXT,
            model         TEXT,
            kullanici     TEXT,
            sifre_sifreli TEXT,
            saglayici     TEXT    NOT NULL DEFAULT 'evrensel',
            aktif         INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS kamera_durumlari (
            kamera_id             INTEGER PRIMARY KEY,
            cevrimici             INTEGER NOT NULL DEFAULT 0,
            ardisik_hata          INTEGER NOT NULL DEFAULT 0,
            gecikme_ms            INTEGER,
            aktif_katman          TEXT    NOT NULL DEFAULT 'evrensel',
            son_kontrol           TEXT    NOT NULL,
            son_basarili_kontrol  TEXT,
            son_mesaj             TEXT,
            FOREIGN KEY (kamera_id) REFERENCES kameralar(id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS kamera_olaylari (
            id              INTEGER PRIMARY KEY AUTOINCREMENT,
            kamera_id       INTEGER NOT NULL,
            tip             TEXT    NOT NULL,
            kaynak          TEXT    NOT NULL DEFAULT 'evrensel',
            mesaj           TEXT,
            basladi         TEXT    NOT NULL,
            bitti           TEXT,
            sure_saniye     INTEGER,
            mail_gonderildi INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (kamera_id) REFERENCES kameralar(id) ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS ix_olay_kamera_zaman
            ON kamera_olaylari (kamera_id, basladi);

        CREATE TABLE IF NOT EXISTS ayarlar (
            anahtar TEXT PRIMARY KEY,
            deger   TEXT
        );

        CREATE TABLE IF NOT EXISTS gecikme_gecmisi (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            kamera_id  INTEGER NOT NULL,
            zaman      TEXT    NOT NULL,
            gecikme_ms INTEGER NOT NULL,
            FOREIGN KEY (kamera_id) REFERENCES kameralar(id) ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS ix_gecikme_kamera_zaman
            ON gecikme_gecmisi (kamera_id, zaman);
        """;
}
