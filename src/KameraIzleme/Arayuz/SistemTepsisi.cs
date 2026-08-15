using KameraIzleme.Modeller;

namespace KameraIzleme.Arayuz;

/// <summary>
/// NotifyIcon sarmalayıcısı. Kapatma butonu tepsiye küçültür,
/// çıkış menüden yapılır; ikon rengi duruma göre değişir.
/// </summary>
public sealed class SistemTepsisi : IDisposable
{
    private readonly Form _pencere;
    private readonly NotifyIcon _ikon;
    private readonly ContextMenuStrip _menu;

    public bool CikisIstenmiyor { get; private set; } = true;

    private readonly Dictionary<long, bool> _oncekiCevrimici = new();

    public SistemTepsisi(Form pencere)
    {
        _pencere = pencere;
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Göster", null, (_, _) => Goster());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Çıkış", null, (_, _) => Cik());

        _ikon = new NotifyIcon
        {
            Text = "Kamera İzleme",
            Visible = true,
            Icon = IkonYap(Color.Gray),
            ContextMenuStrip = _menu,
        };

        _ikon.DoubleClick += (_, _) => Goster();
    }

    public void TepsiDurumunuTazele(IReadOnlyList<(Kamera Kamera, KameraDurumu Durum)> guncellenenler)
    {
        // Yeni düşen kameralar için balon bildir
        var yeniDusenler = new List<Kamera>();
        foreach (var (k, d) in guncellenenler)
        {
            if (_oncekiCevrimici.TryGetValue(k.Id, out bool onceki) && onceki && !d.Cevrimici)
            {
                yeniDusenler.Add(k);
            }

            _oncekiCevrimici[k.Id] = d.Cevrimici;
        }

        if (yeniDusenler.Count == 1)
        {
            BalonGoster("Kamera düştü", $"{yeniDusenler[0].Ad} ({yeniDusenler[0].Ip})", ToolTipIcon.Warning);
        }
        else if (yeniDusenler.Count > 1)
        {
            BalonGoster("Toplu kesinti", $"{yeniDusenler.Count} kamera düştü — ayrıntı için Olaylar sekmesine bakın.", ToolTipIcon.Warning);
        }

        bool hepsiCevrimici = guncellenenler.All(g => g.Item2.Cevrimici);
        bool bazisiDustu = guncellenenler.Any(g => !g.Item2.Cevrimici);
        Color renk = hepsiCevrimici ? Color.SeaGreen : (bazisiDustu ? Color.IndianRed : Color.Goldenrod);

        var yeniIkon = _ikon.Icon;
        _ikon.Icon = IkonYap(renk);
        yeniIkon?.Dispose();
    }

    public void BalonGoster(string baslik, string mesaj, ToolTipIcon tip)
    {
        _ikon.BalloonTipTitle = baslik;
        _ikon.BalloonTipText = mesaj;
        _ikon.BalloonTipIcon = tip;
        _ikon.ShowBalloonTip(4000);
    }

    private void Goster()
    {
        _pencere.Show();
        _pencere.WindowState = FormWindowState.Normal;
        _pencere.BringToFront();
        _pencere.Activate();
    }

    private void Cik()
    {
        CikisIstenmiyor = false;
        _pencere.Close();
    }

    public void Dispose()
    {
        _ikon.Visible = false;
        _ikon.Dispose();
        _menu.Dispose();
    }

    private static Icon IkonYap(Color renk)
    {
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var firca = new SolidBrush(renk);
            g.FillEllipse(firca, 1, 1, 14, 14);
            using var kalem = new Pen(Color.Black, 1);
            g.DrawEllipse(kalem, 1, 1, 14, 14);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }
}
