using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace KameraIzleme.Saglayicilar;

/// <summary>
/// HTTP Digest kimlik doğrulama (RFC 2617/7616).
/// Kameraların çoğunda hâlâ MD5 + qop=auth ile kullanılır.
/// <see cref="HttpClientHandler"/> DigestPreAuthentication'ı tam desteklemediği için
/// elle uyguluyoruz.
/// </summary>
public static class DigestAuth
{
    private static long _ncSayaci;

    /// <summary>
    /// Bir isteği önce anonim yollar, 401 alırsa Digest cevabıyla yeniden gönderir.
    /// Basic auth da desteklenir; <paramref name="kimlikBiciminiZorla"/> ile açıkça istenebilir.
    /// </summary>
    public static async Task<HttpResponseMessage> KimlikliGonderAsync(
        HttpClient istemci,
        Func<HttpRequestMessage> istekUret,
        string? kullanici,
        string? sifre,
        string kimlikBiciminiZorla = "digest",
        CancellationToken ct = default)
    {
        using var ilkIstek = istekUret();
        HttpResponseMessage cevap = await istemci.SendAsync(
            ilkIstek, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (cevap.StatusCode != HttpStatusCode.Unauthorized || string.IsNullOrEmpty(kullanici))
        {
            return cevap;
        }

        // Yeniden dener; ilk cevabı okumuyorsak bile dispose etmek şart.
        cevap.Dispose();

        AuthenticationHeaderValue? meydan = cevap.Headers.WwwAuthenticate
            .FirstOrDefault(h => string.Equals(h.Scheme, "Digest", StringComparison.OrdinalIgnoreCase));

        if (meydan is null || string.Equals(kimlikBiciminiZorla, "basic", StringComparison.OrdinalIgnoreCase))
        {
            // Basic'e düş
            using var basicIstek = istekUret();
            string kimlik = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kullanici}:{sifre}"));
            basicIstek.Headers.Authorization = new AuthenticationHeaderValue("Basic", kimlik);
            return await istemci.SendAsync(basicIstek, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
        }

        var meydanParametreleri = MeydanCoz(meydan.Parameter ?? string.Empty);
        using var yeniIstek = istekUret();
        string yol = yeniIstek.RequestUri!.PathAndQuery;
        string cevapDegeri = DigestCevabiUret(
            kullanici!,
            sifre ?? string.Empty,
            yeniIstek.Method.Method,
            yol,
            meydanParametreleri);
        yeniIstek.Headers.TryAddWithoutValidation("Authorization", cevapDegeri);

        return await istemci.SendAsync(yeniIstek, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
    }

    private static Dictionary<string, string> MeydanCoz(string meydan)
    {
        var sozluk = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int i = 0;
        while (i < meydan.Length)
        {
            while (i < meydan.Length && (meydan[i] == ' ' || meydan[i] == ','))
            {
                i++;
            }

            int esitlik = meydan.IndexOf('=', i);
            if (esitlik < 0)
            {
                break;
            }

            string anahtar = meydan[i..esitlik].Trim();
            i = esitlik + 1;

            string deger;
            if (i < meydan.Length && meydan[i] == '"')
            {
                int kapanan = meydan.IndexOf('"', i + 1);
                if (kapanan < 0)
                {
                    break;
                }

                deger = meydan[(i + 1)..kapanan];
                i = kapanan + 1;
            }
            else
            {
                int virgul = meydan.IndexOf(',', i);
                if (virgul < 0)
                {
                    virgul = meydan.Length;
                }

                deger = meydan[i..virgul].Trim();
                i = virgul;
            }

            sozluk[anahtar] = deger;
        }

        return sozluk;
    }

    private static string DigestCevabiUret(
        string kullanici,
        string sifre,
        string metod,
        string yol,
        IReadOnlyDictionary<string, string> meydan)
    {
        string realm = meydan.GetValueOrDefault("realm", string.Empty);
        string nonce = meydan.GetValueOrDefault("nonce", string.Empty);
        string opaque = meydan.GetValueOrDefault("opaque", string.Empty);
        string qop = meydan.GetValueOrDefault("qop", string.Empty).Split(',')[0].Trim();
        string algoritma = meydan.GetValueOrDefault("algorithm", "MD5").Trim();

        string ha1 = Md5($"{kullanici}:{realm}:{sifre}");
        string ha2 = Md5($"{metod}:{yol}");

        string cnonce = RandomNumberGenerator.GetHexString(16, lowercase: true);
        long ncNum = Interlocked.Increment(ref _ncSayaci);
        string nc = ncNum.ToString("x8");

        string response;
        string qopKismi = string.Empty;
        if (!string.IsNullOrEmpty(qop))
        {
            response = Md5($"{ha1}:{nonce}:{nc}:{cnonce}:{qop}:{ha2}");
            qopKismi = $", qop={qop}, nc={nc}, cnonce=\"{cnonce}\"";
        }
        else
        {
            response = Md5($"{ha1}:{nonce}:{ha2}");
        }

        var sb = new StringBuilder("Digest ");
        sb.Append($"username=\"{kullanici}\", ");
        sb.Append($"realm=\"{realm}\", ");
        sb.Append($"nonce=\"{nonce}\", ");
        sb.Append($"uri=\"{yol}\", ");
        sb.Append($"algorithm={algoritma}, ");
        sb.Append($"response=\"{response}\"");
        if (!string.IsNullOrEmpty(opaque))
        {
            sb.Append($", opaque=\"{opaque}\"");
        }

        sb.Append(qopKismi);
        return sb.ToString();
    }

    private static string Md5(string metin)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(metin));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
