using KameraIzleme.Izleme;
using KameraIzleme.Mail;
using KameraIzleme.Saglayicilar;
using KameraIzleme.Veri;

namespace KameraIzleme.Arayuz;

/// <summary>
/// Uygulama boyunca kullanılan tekil örnekleri tutan basit bir kapsayıcı.
/// Aşırı soyutlama olmasın diye <c>Microsoft.Extensions.DependencyInjection</c>'ı
/// tercih etmiyoruz — sadece iki tıklık el yordamı bağlama yeter.
/// </summary>
public sealed class UygulamaKapsami : IAsyncDisposable
{
    public AyarlarDeposu Ayarlar { get; }
    public KameraDeposu Kameralar { get; }
    public NvrDeposu Nvrlar { get; }
    public DurumDeposu Durumlar { get; }
    public OlayDeposu Olaylar { get; }
    public MailGondericisi Mail { get; }
    public SaglayiciSecici Secici { get; }
    public IReadOnlyList<SaglayiciTanimi> Tanimlar { get; private set; }
    public AlarmMotoru Alarm { get; }
    public IzlemeServisi Izleme { get; }
    public Deneme.SahteHttpSunucu? DenemeHttp { get; private set; }
    public Deneme.SahteOnvifSunucu? DenemeOnvif { get; private set; }

    public static UygulamaKapsami Olustur()
    {
        var ayarlar = new AyarlarDeposu();
        var kameralar = new KameraDeposu();
        var nvrlar = new NvrDeposu();
        var durumlar = new DurumDeposu();
        var olaylar = new OlayDeposu();
        var mail = new MailGondericisi(ayarlar);
        var secici = SaglayiciKayit.SeciciyiKur(ayarlar, out var tanimlar);
        var alarm = new AlarmMotoru(ayarlar, olaylar, durumlar, kameralar, mail);
        var izleme = new IzlemeServisi(ayarlar, kameralar, durumlar, olaylar, secici, alarm);

        return new UygulamaKapsami(
            ayarlar, kameralar, nvrlar, durumlar, olaylar, mail, secici, tanimlar, alarm, izleme);
    }

    private UygulamaKapsami(
        AyarlarDeposu ayarlar,
        KameraDeposu kameralar,
        NvrDeposu nvrlar,
        DurumDeposu durumlar,
        OlayDeposu olaylar,
        MailGondericisi mail,
        SaglayiciSecici secici,
        IReadOnlyList<SaglayiciTanimi> tanimlar,
        AlarmMotoru alarm,
        IzlemeServisi izleme)
    {
        Ayarlar = ayarlar;
        Kameralar = kameralar;
        Nvrlar = nvrlar;
        Durumlar = durumlar;
        Olaylar = olaylar;
        Mail = mail;
        Secici = secici;
        Tanimlar = tanimlar;
        Alarm = alarm;
        Izleme = izleme;
    }

    public void SaglayicilariYenidenYukle()
    {
        var yeniSecici = SaglayiciKayit.SeciciyiKur(Ayarlar, out var yeniTanimlar);
        Tanimlar = yeniTanimlar;
        // Not: Seçici tekil olduğu için form içindeki listeler yeniden okunmak zorunda.
        // Bu prototip için basit yol: uygulamayı yeniden başlatmaya davet eden bir bildirimle idare edilir.
    }

    public void DenemeSunucularaBasla()
    {
        if (!Ayarlar.Al("uygulama.deneme_modu", false))
        {
            return;
        }

        DenemeHttp ??= new Deneme.SahteHttpSunucu();
        DenemeOnvif ??= new Deneme.SahteOnvifSunucu();
        DenemeHttp.Basla();
        DenemeOnvif.Basla();

        Deneme.DenemeVerileri.EkleEksikleriEkle(Kameralar, Nvrlar);
    }

    public async ValueTask DisposeAsync()
    {
        await Izleme.DisposeAsync().ConfigureAwait(false);
        DenemeHttp?.Dispose();
        DenemeOnvif?.Dispose();
    }
}
