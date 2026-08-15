using KameraIzleme.Arayuz.Sekmeler;
using Serilog;

namespace KameraIzleme.Arayuz;

public partial class AnaForm : Form
{
    private readonly UygulamaKapsami _kapsam;
    private readonly SistemTepsisi _tepsi;
    private readonly CanliDurumPaneli _canliPanel;
    private readonly KameralarPaneli _kameralarPanel;
    private readonly OlaylarPaneli _olaylarPanel;
    private readonly AyarlarPaneli _ayarlarPanel;

    public AnaForm()
    {
        _kapsam = UygulamaKapsami.Olustur();

        InitializeComponent();
        Text = "Kamera İzleme";
        MinimumSize = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterScreen;

        _canliPanel = new CanliDurumPaneli(_kapsam) { Dock = DockStyle.Fill };
        _kameralarPanel = new KameralarPaneli(_kapsam) { Dock = DockStyle.Fill };
        _olaylarPanel = new OlaylarPaneli(_kapsam) { Dock = DockStyle.Fill };
        _ayarlarPanel = new AyarlarPaneli(_kapsam) { Dock = DockStyle.Fill };

        sekmeCanli.Controls.Add(_canliPanel);
        sekmeKameralar.Controls.Add(_kameralarPanel);
        sekmeOlaylar.Controls.Add(_olaylarPanel);
        sekmeAyarlar.Controls.Add(_ayarlarPanel);

        _tepsi = new SistemTepsisi(this);
        _tepsi.KameraDusuncePatlat += (_, mesaj) => _tepsi.BalonGoster("Kamera düştü", mesaj, ToolTipIcon.Warning);

        // Deneme modu (varsa) sunucularını başlat
        _kapsam.DenemeSunucularaBasla();

        // Ana izleme servisini başlat
        _kapsam.Izleme.Basla();
        _kapsam.Izleme.DurumGuncellendi += (s, a) => this.BeginInvoke(() =>
        {
            _canliPanel.DurumlariUygula(a.Guncellenenler);
            _tepsi.TepsiDurumunuTazele(a.Guncellenenler);
        });

        FormClosing += AnaForm_FormClosing;
        Log.Information("AnaForm hazır");
    }

    private void AnaForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Kapatma X butonu tepsiye küçültür; çıkış tepsi menüsünden.
        if (e.CloseReason == CloseReason.UserClosing && _tepsi.CikisIstenmiyor)
        {
            e.Cancel = true;
            Hide();
            _tepsi.BalonGoster("Kamera İzleme", "Uygulama tepsiye küçültüldü.", ToolTipIcon.Info);
            return;
        }

        // Gerçek kapanış
        Task.Run(async () => await _kapsam.DisposeAsync()).GetAwaiter().GetResult();
        _tepsi.Dispose();
    }
}
