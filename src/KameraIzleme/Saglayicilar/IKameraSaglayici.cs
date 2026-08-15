using KameraIzleme.Modeller;

namespace KameraIzleme.Saglayicilar;

/// <summary>
/// Kamera sağlayıcı sözleşmesi. Üç uygulama vardır:
/// <see cref="EvrenselSaglayici"/> (öncelik 0), <see cref="OnvifSaglayici"/> (öncelik 50)
/// ve <see cref="TanimliSaglayici"/> (JSON'dan gelen öncelik, tipik 100).
/// </summary>
public interface IKameraSaglayici
{
    string Marka { get; }
    int Oncelik { get; }

    /// <summary>Cihazın bu sağlayıcıya uygun olup olmadığını sınar (parmak izi).</summary>
    Task<bool> DesteklerMiAsync(CihazBilgisi cihaz, CancellationToken ct);

    /// <summary>Anlık durum kontrolü — bir turluk sonucu döner.</summary>
    Task<KontrolSonucu> DurumKontrolAsync(Kamera kamera, CancellationToken ct);

    /// <summary>NVR kanal listesi (varsa). Uygulanamıyorsa boş liste döner.</summary>
    Task<IReadOnlyList<KanalBilgisi>> KanallariGetirAsync(Kamera kamera, CancellationToken ct);

    /// <summary>Cihazdan gelen olay akışı. Uygulanamıyorsa boş akış döner.</summary>
    IAsyncEnumerable<CihazOlayi> OlaylariDinleAsync(Kamera kamera, CancellationToken ct);
}
