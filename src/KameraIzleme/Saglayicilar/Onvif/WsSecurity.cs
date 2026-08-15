using System.Security.Cryptography;
using System.Text;

namespace KameraIzleme.Saglayicilar.Onvif;

/// <summary>
/// ONVIF isteklerinin SOAP header'ına eklenecek WS-Security UsernameToken (digest profili).
/// PasswordDigest = Base64(SHA1(Base64(Nonce) + Created + Password))  (spesifikasyon: Nonce ham baytların Base64'ünde).
/// </summary>
public static class WsSecurity
{
    public static string UsernameTokenXml(string kullanici, string sifre)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(16);
        string nonceB64 = Convert.ToBase64String(nonce);
        string created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        byte[] birlesmis = nonce
            .Concat(Encoding.UTF8.GetBytes(created))
            .Concat(Encoding.UTF8.GetBytes(sifre))
            .ToArray();
        byte[] hash = SHA1.HashData(birlesmis);
        string digest = Convert.ToBase64String(hash);

        return $"""
            <wsse:Security xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd">
              <wsse:UsernameToken>
                <wsse:Username>{XmlKacir(kullanici)}</wsse:Username>
                <wsse:Password Type="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest">{digest}</wsse:Password>
                <wsse:Nonce EncodingType="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary">{nonceB64}</wsse:Nonce>
                <wsu:Created>{created}</wsu:Created>
              </wsse:UsernameToken>
            </wsse:Security>
            """;
    }

    public static string XmlKacir(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
         .Replace("\"", "&quot;").Replace("'", "&apos;");
}
