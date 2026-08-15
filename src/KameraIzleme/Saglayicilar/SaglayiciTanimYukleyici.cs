using System.Text.Json;
using Serilog;

namespace KameraIzleme.Saglayicilar;

/// <summary>
/// <c>Saglayicilar/Tanimlar/</c> klasöründeki tüm JSON dosyalarını okur.
/// Yeni marka eklemek isteyen kullanıcı buraya bir dosya kopyalar ve
/// "yeniden yükle" der; kod değişikliği gerekmez.
/// </summary>
public sealed class SaglayiciTanimYukleyici
{
    private static readonly JsonSerializerOptions Secenekler = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Klasöre bakılacak varsayılan yol.</summary>
    public static string VarsayilanDizin =>
        Path.Combine(AppContext.BaseDirectory, "Saglayicilar", "Tanimlar");

    /// <summary>Verilen klasördeki tüm tanımları yükler; hatalı dosyaları loglar ama atlar.</summary>
    public IReadOnlyList<SaglayiciTanimi> Yukle(string? dizin = null)
    {
        dizin ??= VarsayilanDizin;

        if (!Directory.Exists(dizin))
        {
            Log.Information("Sağlayıcı tanım klasörü bulunamadı, oluşturuluyor: {D}", dizin);
            Directory.CreateDirectory(dizin);
            return Array.Empty<SaglayiciTanimi>();
        }

        var sonuc = new List<SaglayiciTanimi>();
        foreach (string dosya in Directory.EnumerateFiles(dizin, "*.json"))
        {
            try
            {
                string icerik = File.ReadAllText(dosya);
                var tanim = JsonSerializer.Deserialize<SaglayiciTanimi>(icerik, Secenekler);
                if (tanim is null || string.IsNullOrWhiteSpace(tanim.Marka))
                {
                    Log.Warning("Sağlayıcı tanımı geçersiz (marka boş): {D}", dosya);
                    continue;
                }

                sonuc.Add(tanim);
                Log.Information("Sağlayıcı tanımı yüklendi: {M} (öncelik {O})", tanim.Marka, tanim.Oncelik);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Sağlayıcı tanımı yüklenemedi: {D}", dosya);
            }
        }

        return sonuc;
    }
}
