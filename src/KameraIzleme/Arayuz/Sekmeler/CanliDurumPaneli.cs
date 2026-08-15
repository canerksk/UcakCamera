using KameraIzleme.Modeller;

namespace KameraIzleme.Arayuz.Sekmeler;

public partial class CanliDurumPaneli : UserControl
{
    private readonly UygulamaKapsami _kapsam;
    private readonly Dictionary<long, KameraKarti> _kartlar = new();

    public CanliDurumPaneli(UygulamaKapsami kapsam)
    {
        _kapsam = kapsam;
        InitializeComponent();
        KartlariHazirla();
        cmbFiltre.SelectedIndexChanged += (_, _) => KartlariHazirla();
        cmbKatman.SelectedIndexChanged += (_, _) => KartlariHazirla();
        cmbMarka.SelectedIndexChanged += (_, _) => KartlariHazirla();
        txtArama.TextChanged += (_, _) => KartlariHazirla();
        btnSimdi.Click += async (_, _) => await SimdiKontrolAsync();
    }

    /// <summary>İzleme servisinin verdiği tur sonuçlarını UI thread'inde uygular.</summary>
    public void DurumlariUygula(IReadOnlyList<(Kamera Kamera, KameraDurumu Durum)> guncellenenler)
    {
        foreach (var (k, d) in guncellenenler)
        {
            if (_kartlar.TryGetValue(k.Id, out var kart))
            {
                kart.DurumuGuncelle(d);
            }
        }

        OzetTazele();
    }

    private void KartlariHazirla()
    {
        panelKartlar.SuspendLayout();
        panelKartlar.Controls.Clear();
        _kartlar.Clear();

        var kameralar = _kapsam.Kameralar.Tumu();
        var durumlar = _kapsam.Durumlar.Tumu().ToDictionary(d => d.KameraId, d => d);
        var lokasyonlar = kameralar.Select(k => k.Lokasyon ?? "(yok)").Distinct().OrderBy(x => x).ToList();
        DoldurCombo(cmbFiltre, "(tüm lokasyonlar)", lokasyonlar);

        var markalar = kameralar.Select(k => k.Marka ?? "(bilinmiyor)").Distinct().OrderBy(x => x).ToList();
        DoldurCombo(cmbMarka, "(tüm markalar)", markalar);

        var katmanlar = new List<string> { "evrensel", "onvif" };
        katmanlar.AddRange(_kapsam.Tanimlar.Select(t => t.Marka));
        DoldurCombo(cmbKatman, "(tüm katmanlar)", katmanlar.Distinct().ToList());

        foreach (var k in Filtrele(kameralar))
        {
            durumlar.TryGetValue(k.Id, out var d);
            var kart = new KameraKarti(k, d);
            _kartlar[k.Id] = kart;
            panelKartlar.Controls.Add(kart);
        }

        panelKartlar.ResumeLayout();
        OzetTazele();
    }

    private static void DoldurCombo(ComboBox cmb, string ilk, IReadOnlyList<string> ogeler)
    {
        string? secili = cmb.SelectedItem?.ToString();
        cmb.Items.Clear();
        cmb.Items.Add(ilk);
        foreach (var o in ogeler)
        {
            cmb.Items.Add(o);
        }

        int idx = cmb.Items.IndexOf(secili ?? ilk);
        cmb.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private IEnumerable<Kamera> Filtrele(IReadOnlyList<Kamera> hepsi)
    {
        string arama = txtArama.Text.Trim();
        string? lokasyon = cmbFiltre.SelectedIndex > 0 ? cmbFiltre.SelectedItem?.ToString() : null;
        string? marka = cmbMarka.SelectedIndex > 0 ? cmbMarka.SelectedItem?.ToString() : null;
        string? katman = cmbKatman.SelectedIndex > 0 ? cmbKatman.SelectedItem?.ToString() : null;

        var durumlar = _kapsam.Durumlar.Tumu().ToDictionary(d => d.KameraId);
        return hepsi.Where(k =>
        {
            if (!string.IsNullOrEmpty(arama)
                && !k.Ad.Contains(arama, StringComparison.OrdinalIgnoreCase)
                && !k.Ip.Contains(arama, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (lokasyon != null && (k.Lokasyon ?? "(yok)") != lokasyon) return false;
            if (marka != null && (k.Marka ?? "(bilinmiyor)") != marka) return false;
            if (katman != null)
            {
                var d = durumlar.GetValueOrDefault(k.Id);
                if (!string.Equals(d?.AktifKatman ?? k.Saglayici, katman, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        });
    }

    private void OzetTazele()
    {
        var kameralar = _kapsam.Kameralar.Tumu();
        var durumlar = _kapsam.Durumlar.Tumu();
        int toplam = kameralar.Count;
        int cevrimici = durumlar.Count(d => d.Cevrimici);
        int cevrimdisi = durumlar.Count(d => !d.Cevrimici);
        var son24 = _kapsam.Olaylar.Filtrele(baslangic: DateTime.UtcNow.AddHours(-24), limit: 100000).Count;
        etOzet.Text = $"Toplam: {toplam}  ·  Çevrimiçi: {cevrimici}  ·  Çevrimdışı: {cevrimdisi}  ·  Son 24 sa olay: {son24}";
    }

    private async Task SimdiKontrolAsync()
    {
        btnSimdi.Enabled = false;
        try
        {
            foreach (var k in _kapsam.Kameralar.Tumu())
            {
                try
                {
                    await _kapsam.Izleme.SimdiKontrolEtAsync(k.Id, default);
                }
                catch { /* tek kamera hatasını yut */ }
            }
        }
        finally
        {
            btnSimdi.Enabled = true;
        }
    }
}
