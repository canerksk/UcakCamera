using System.Collections.Concurrent;
using System.Globalization;
using Dapper;

namespace KameraIzleme.Veri;

/// <summary>
/// Basit anahtar/değer ayar deposu. Sık okunacağı için sadelik adına
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> ile bellekte önbelleklenir.
/// </summary>
public sealed class AyarlarDeposu
{
    private readonly ConcurrentDictionary<string, string?> _onbellek = new();
    private bool _yuklendi;

    public string? Al(string anahtar, string? varsayilan = null)
    {
        YukleGerekliyse();
        return _onbellek.TryGetValue(anahtar, out var deger) ? deger : varsayilan;
    }

    public int Al(string anahtar, int varsayilan)
    {
        var d = Al(anahtar);
        return int.TryParse(d, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : varsayilan;
    }

    public bool Al(string anahtar, bool varsayilan)
    {
        var d = Al(anahtar);
        return bool.TryParse(d, out var s) ? s : varsayilan;
    }

    public double Al(string anahtar, double varsayilan)
    {
        var d = Al(anahtar);
        return double.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s : varsayilan;
    }

    public void Ata(string anahtar, string? deger)
    {
        using var b = VeriTabani.Ac();
        b.Execute(
            """
            INSERT INTO ayarlar (anahtar, deger) VALUES (@A, @D)
            ON CONFLICT(anahtar) DO UPDATE SET deger = excluded.deger
            """, new { A = anahtar, D = deger });
        _onbellek[anahtar] = deger;
    }

    public void Ata(string anahtar, int deger) => Ata(anahtar, deger.ToString(CultureInfo.InvariantCulture));

    public void Ata(string anahtar, bool deger) => Ata(anahtar, deger ? "true" : "false");

    public void Ata(string anahtar, double deger) => Ata(anahtar, deger.ToString(CultureInfo.InvariantCulture));

    public IReadOnlyDictionary<string, string?> Tumu()
    {
        YukleGerekliyse();
        return _onbellek.ToDictionary(x => x.Key, x => x.Value);
    }

    public void OnbellekYenile()
    {
        _onbellek.Clear();
        _yuklendi = false;
        YukleGerekliyse();
    }

    private void YukleGerekliyse()
    {
        if (_yuklendi)
        {
            return;
        }

        using var b = VeriTabani.Ac();
        var satirlar = b.Query<(string A, string? D)>("SELECT anahtar AS A, deger AS D FROM ayarlar");
        foreach (var (a, d) in satirlar)
        {
            _onbellek[a] = d;
        }

        _yuklendi = true;
    }
}
