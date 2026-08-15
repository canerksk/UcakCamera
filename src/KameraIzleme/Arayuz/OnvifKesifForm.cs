using KameraIzleme.Modeller;
using KameraIzleme.Saglayicilar.Onvif;

namespace KameraIzleme.Arayuz;

/// <summary>WS-Discovery ile ağdaki ONVIF cihazlarını bulur ve seçilenleri kaydeder.</summary>
public partial class OnvifKesifForm : Form
{
    private readonly UygulamaKapsami _kapsam;
    private readonly List<KesfedilenCihaz> _bulunanlar = new();

    public OnvifKesifForm(UygulamaKapsami kapsam)
    {
        _kapsam = kapsam;
        InitializeComponent();
        btnAra.Click += async (_, _) => await AraAsync();
        btnEkle.Click += (_, _) => Ekle();
    }

    private async Task AraAsync()
    {
        btnAra.Enabled = false;
        grid.Rows.Clear();
        _bulunanlar.Clear();
        try
        {
            var kesif = new WsDiscovery();
            var sonuc = await kesif.KesifYapAsync(TimeSpan.FromSeconds(5), default);

            var onvif = new Saglayicilar.OnvifSaglayici();
            foreach (var c in sonuc)
            {
                // Cihazdan üretici/model çek, RTSP URI'yı sor
                var bilgi = await onvif.CihazBilgisiAlAsync(c.Adres, c.Port, kullanici: null, sifre: null, default);
                string? rtsp = await onvif.RtspUrlAlAsync(c.Adres, c.Port, kullanici: null, sifre: null, default);
                c.Uretici = bilgi?.OnvifUretici;
                c.Model = bilgi?.OnvifModel;
                c.SeriNo = bilgi?.OnvifSeriNo;
                c.RtspUrl = rtsp;

                _bulunanlar.Add(c);
                grid.Rows.Add(false, c.Adres, c.Port, c.Uretici, c.Model, c.RtspUrl);
            }
        }
        finally
        {
            btnAra.Enabled = true;
        }
    }

    private void Ekle()
    {
        int eklendi = 0;
        for (int i = 0; i < grid.Rows.Count; i++)
        {
            if (grid.Rows[i].Cells[0].Value is not true) continue;
            var c = _bulunanlar[i];
            var mevcut = _kapsam.Kameralar.IpIleGetir(c.Adres);
            if (mevcut is not null) continue;

            var rtspYol = "/";
            int rtspPort = 554;
            if (!string.IsNullOrEmpty(c.RtspUrl) && Uri.TryCreate(c.RtspUrl, UriKind.Absolute, out var u))
            {
                rtspYol = u.PathAndQuery;
                rtspPort = u.Port > 0 ? u.Port : 554;
            }

            _kapsam.Kameralar.Ekle(new Kamera
            {
                Ad = c.Uretici + " " + c.Adres,
                Ip = c.Adres,
                OnvifPort = c.Port,
                HttpPort = c.Port == 80 ? 80 : c.Port,
                RtspPort = rtspPort,
                RtspAnaAkis = rtspYol,
                Marka = c.Uretici,
                Model = c.Model,
                SeriNo = c.SeriNo,
                Saglayici = "onvif",
            });
            eklendi++;
        }

        MessageBox.Show($"{eklendi} kamera eklendi.", "ONVIF Keşif");
        if (eklendi > 0)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
