using System.Security.Cryptography;
using System.Text;

namespace KameraIzleme.Veri;

/// <summary>
/// DPAPI (kullanıcı kapsamı) ile şifre koruma. Uygulama yalnızca Windows olduğu için
/// <see cref="ProtectedData"/> her zaman kullanılabilir.
/// </summary>
public static class SifreKorumasi
{
    private static readonly byte[] EkEntropi =
        Encoding.UTF8.GetBytes("KameraIzleme.v1.EkEntropi");

    /// <summary>Düz metin şifreyi Base64 formatında şifreler.</summary>
    public static string? Sifrele(string? duzMetin)
    {
        if (string.IsNullOrEmpty(duzMetin))
        {
            return null;
        }

        byte[] veri = Encoding.UTF8.GetBytes(duzMetin);
        byte[] korunmus = ProtectedData.Protect(veri, EkEntropi, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(korunmus);
    }

    /// <summary>Base64 şifreli metni geri açar; boş / geçersiz ise null döner.</summary>
    public static string? Coz(string? sifreli)
    {
        if (string.IsNullOrEmpty(sifreli))
        {
            return null;
        }

        try
        {
            byte[] veri = Convert.FromBase64String(sifreli);
            byte[] cozulmus = ProtectedData.Unprotect(veri, EkEntropi, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(cozulmus);
        }
        catch (Exception)
        {
            // Şifre farklı bir kullanıcıda korunmuş olabilir — boş dönüp uygulamayı çökertme.
            return null;
        }
    }
}
