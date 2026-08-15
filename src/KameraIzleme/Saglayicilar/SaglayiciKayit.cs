using KameraIzleme.Veri;

namespace KameraIzleme.Saglayicilar;

/// <summary>Uygulama açılışında tüm sağlayıcıları hazırlayıp seçiciyi kuran yardımcı.</summary>
public static class SaglayiciKayit
{
    public static SaglayiciSecici SeciciyiKur(AyarlarDeposu ayarlar, out List<SaglayiciTanimi> tanimlar)
    {
        var yukleyici = new SaglayiciTanimYukleyici();
        tanimlar = yukleyici.Yukle().ToList();

        var liste = new List<IKameraSaglayici>
        {
            new EvrenselSaglayici(ayarlar),
            new OnvifSaglayici(),
        };

        foreach (var tanim in tanimlar)
        {
            liste.Add(new TanimliSaglayici(tanim, ayarlar));
        }

        return new SaglayiciSecici(liste);
    }
}
