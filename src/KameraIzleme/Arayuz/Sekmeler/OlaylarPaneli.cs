using KameraIzleme.Modeller;
using ScottPlot.WinForms;

namespace KameraIzleme.Arayuz.Sekmeler;

public partial class OlaylarPaneli : UserControl
{
    private readonly UygulamaKapsami _kapsam;

    public OlaylarPaneli(UygulamaKapsami kapsam)
    {
        _kapsam = kapsam;
        InitializeComponent();
        btnUygula.Click += (_, _) => Yenile();
        btnCsv.Click += (_, _) => CsvDisari();
        cmbKamera.SelectedIndexChanged += (_, _) => GrafikYenile();
        Load += (_, _) => { KameralariDoldur(); Yenile(); };
    }

    private void KameralariDoldur()
    {
        cmbKamera.Items.Clear();
        cmbKamera.Items.Add("(hepsi)");
        foreach (var k in _kapsam.Kameralar.Tumu())
        {
            cmbKamera.Items.Add(new KameraOgesi(k));
        }

        cmbKamera.SelectedIndex = 0;
    }

    private void Yenile()
    {
        DateTime b = dtBas.Value.ToUniversalTime();
        DateTime e = dtBit.Value.ToUniversalTime();
        long? id = null;
        if (cmbKamera.SelectedIndex > 0 && cmbKamera.SelectedItem is KameraOgesi ko)
        {
            id = ko.Kamera.Id;
        }

        var olaylar = _kapsam.Olaylar.Filtrele(b, e, id, tip: null, kaynak: null, 5000);
        gridOlaylar.Rows.Clear();
        var kameraSozlugu = _kapsam.Kameralar.Tumu().ToDictionary(x => x.Id, x => x);
        foreach (var o in olaylar)
        {
            var k = kameraSozlugu.GetValueOrDefault(o.KameraId);
            gridOlaylar.Rows.Add(
                o.Basladi.ToLocalTime(),
                k?.Ad ?? o.KameraId.ToString(),
                o.Tip,
                o.Kaynak,
                o.Bitti?.ToLocalTime().ToString() ?? "(devam)",
                o.SureSaniye,
                o.Mesaj);
        }

        UptimeTablosuDoldur();
        EnSorunlularDoldur();
        GrafikYenile();
    }

    private void UptimeTablosuDoldur()
    {
        gridUptime.Rows.Clear();
        var kameralar = _kapsam.Kameralar.Tumu();

        foreach (var k in kameralar)
        {
            gridUptime.Rows.Add(
                k.Ad,
                UptimeYuzde(k.Id, TimeSpan.FromHours(24)).ToString("0.00") + " %",
                UptimeYuzde(k.Id, TimeSpan.FromDays(7)).ToString("0.00") + " %",
                UptimeYuzde(k.Id, TimeSpan.FromDays(30)).ToString("0.00") + " %");
        }
    }

    private double UptimeYuzde(long kameraId, TimeSpan pencere)
    {
        DateTime bitis = DateTime.UtcNow;
        DateTime baslangic = bitis - pencere;
        long kesintiSn = _kapsam.Olaylar.KesintiSuresiSaniye(kameraId, baslangic, bitis);
        double toplamSn = pencere.TotalSeconds;
        if (toplamSn <= 0) return 100;
        double oran = 100.0 * (toplamSn - kesintiSn) / toplamSn;
        return Math.Clamp(oran, 0, 100);
    }

    private void EnSorunlularDoldur()
    {
        gridSorunlu.Rows.Clear();
        var kameraSozlugu = _kapsam.Kameralar.Tumu().ToDictionary(k => k.Id, k => k);
        foreach (var (id, sayi) in _kapsam.Olaylar.EnSikDusenler())
        {
            var k = kameraSozlugu.GetValueOrDefault(id);
            gridSorunlu.Rows.Add(k?.Ad ?? id.ToString(), sayi);
        }
    }

    private void GrafikYenile()
    {
        grafik.Plot.Clear();
        if (cmbKamera.SelectedIndex <= 0)
        {
            grafik.Refresh();
            return;
        }

        var ko = (KameraOgesi)cmbKamera.SelectedItem!;
        var seri = _kapsam.Durumlar.GecikmeGetir(ko.Kamera.Id, DateTime.UtcNow.AddHours(-24));
        if (seri.Count == 0)
        {
            grafik.Refresh();
            return;
        }

        double[] x = seri.Select(s => s.Zaman.ToLocalTime().ToOADate()).ToArray();
        double[] y = seri.Select(s => (double)s.GecikmeMs).ToArray();
        var sc = grafik.Plot.Add.Scatter(x, y);
        sc.LegendText = "Ping (ms)";
        grafik.Plot.Axes.DateTimeTicksBottom();
        grafik.Plot.Title($"{ko.Kamera.Ad} — son 24 saat gecikme");
        grafik.Refresh();
    }

    private void CsvDisari()
    {
        using var sf = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "olaylar.csv" };
        if (sf.ShowDialog(this) != DialogResult.OK) return;
        using var yaz = new StreamWriter(sf.FileName);
        yaz.WriteLine("basladi,kamera,tip,kaynak,bitti,sure_saniye,mesaj");
        foreach (DataGridViewRow r in gridOlaylar.Rows)
        {
            yaz.WriteLine(string.Join(',',
                r.Cells[0].Value,
                CsvKacir(r.Cells[1].Value?.ToString() ?? ""),
                r.Cells[2].Value,
                r.Cells[3].Value,
                r.Cells[4].Value,
                r.Cells[5].Value,
                CsvKacir(r.Cells[6].Value?.ToString() ?? "")));
        }
    }

    private static string CsvKacir(string s) => s.Contains(',') || s.Contains('"')
        ? "\"" + s.Replace("\"", "\"\"") + "\""
        : s;

    private sealed record KameraOgesi(Kamera Kamera)
    {
        public override string ToString() => Kamera.Ad;
    }
}
