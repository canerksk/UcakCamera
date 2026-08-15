using System.Reflection;
using KameraIzleme.Saglayicilar;
using Xunit;

namespace KameraIzleme.Tests;

public class DigestAuthTestleri
{
    // MeydanCoz özel; test için Reflection ile çağırıyoruz.
    private static Dictionary<string, string> MeydanCoz(string s)
    {
        var t = typeof(DigestAuth);
        var m = t.GetMethod("MeydanCoz", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (Dictionary<string, string>)m.Invoke(null, new object[] { s })!;
    }

    [Fact]
    public void Bosluklu_ve_tirnakli_meydani_dogru_cozer()
    {
        string s = "realm=\"IPC\", qop=\"auth\", nonce=\"abc123\", algorithm=MD5, opaque=\"o\"";
        var d = MeydanCoz(s);
        Assert.Equal("IPC", d["realm"]);
        Assert.Equal("auth", d["qop"]);
        Assert.Equal("abc123", d["nonce"]);
        Assert.Equal("MD5", d["algorithm"]);
        Assert.Equal("o", d["opaque"]);
    }
}
