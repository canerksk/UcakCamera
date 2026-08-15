using System.Drawing.Drawing2D;
using KameraIzleme.Modeller;

namespace KameraIzleme.Arayuz;

/// <summary>Canlı durum sekmesindeki bir kamerayı temsil eden kart.</summary>
public partial class KameraKarti : UserControl
{
    public Kamera Kamera { get; }
    public KameraDurumu Durum { get; private set; }

    public KameraKarti(Kamera kamera, KameraDurumu? durum)
    {
        Kamera = kamera;
        Durum = durum ?? new KameraDurumu { KameraId = kamera.Id };
        InitializeComponent();
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        UyariUygula();
    }

    /// <summary>Kart görselini yeni durumla yeniler.</summary>
    public void DurumuGuncelle(KameraDurumu yeniDurum)
    {
        Durum = yeniDurum;
        UyariUygula();
        Invalidate();
    }

    private void UyariUygula()
    {
        etAd.Text = Kamera.Ad;
        etIp.Text = $"{Kamera.Ip} · {Kamera.Lokasyon}";

        string marka = string.IsNullOrEmpty(Kamera.Marka) ? "?" : Kamera.Marka;
        etMarka.Text = $"[{marka}] {Durum.AktifKatman}";

        if (Durum.SonKontrol == default)
        {
            etDurum.Text = "Henüz kontrol yok";
            etDurum.ForeColor = Color.Gray;
            renkSeridi.BackColor = Color.Gray;
            return;
        }

        if (Durum.Cevrimici)
        {
            etDurum.Text = Durum.GecikmeMs is int g ? $"Çevrimiçi · {g} ms" : "Çevrimiçi";
            etDurum.ForeColor = Color.FromArgb(30, 130, 60);
            renkSeridi.BackColor = Color.FromArgb(60, 170, 90);
        }
        else
        {
            var sure = DateTime.UtcNow - (Durum.SonBasariliKontrol ?? Durum.SonKontrol);
            etDurum.Text = $"Çevrimdışı · {InsanaOkunurSure(sure)}";
            etDurum.ForeColor = Color.FromArgb(180, 40, 40);
            renkSeridi.BackColor = Color.FromArgb(210, 60, 60);
        }
    }

    private static string InsanaOkunurSure(TimeSpan s)
    {
        if (s.TotalSeconds < 60) return $"{(int)s.TotalSeconds} sn";
        if (s.TotalMinutes < 60) return $"{(int)s.TotalMinutes} dk";
        if (s.TotalHours < 24) return $"{(int)s.TotalHours} sa";
        return $"{(int)s.TotalDays} gün";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Yumuşak köşe efekti
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var kenar = new Pen(Color.FromArgb(220, 220, 220));
        e.Graphics.DrawRectangle(kenar, 0, 0, Width - 1, Height - 1);
    }
}
