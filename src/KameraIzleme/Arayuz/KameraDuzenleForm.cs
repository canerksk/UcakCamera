using KameraIzleme.Modeller;
using KameraIzleme.Saglayicilar;
using KameraIzleme.Veri;

namespace KameraIzleme.Arayuz;

public partial class KameraDuzenleForm : Form
{
    private readonly UygulamaKapsami _kapsam;
    private readonly Kamera _kamera;

    public KameraDuzenleForm(UygulamaKapsami kapsam, Kamera? mevcut)
    {
        _kapsam = kapsam;
        _kamera = mevcut ?? new Kamera();
        InitializeComponent();
        Text = mevcut is null ? "Yeni kamera" : "Kamera düzenle";

        cmbSaglayici.Items.Add("(otomatik)");
        foreach (var s in _kapsam.Secici.BilinenSaglayiciAdlari)
        {
            cmbSaglayici.Items.Add(s);
        }

        Doldur();
        btnKaydet.Click += (_, _) => Kaydet();
        btnIptal.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnTest.Click += async (_, _) => await BaglantiTestEtAsync();
    }

    private void Doldur()
    {
        txtAd.Text = _kamera.Ad;
        txtIp.Text = _kamera.Ip;
        txtRtspPort.Text = _kamera.RtspPort.ToString();
        txtHttpPort.Text = _kamera.HttpPort.ToString();
        txtOnvifPort.Text = _kamera.OnvifPort.ToString();
        txtRtspAna.Text = _kamera.RtspAnaAkis ?? "";
        txtRtspAlt.Text = _kamera.RtspAltAkis ?? "";
        txtKullanici.Text = _kamera.Kullanici ?? "";
        txtSifre.Text = SifreKorumasi.Coz(_kamera.SifreSifreli) ?? "";
        txtLokasyon.Text = _kamera.Lokasyon ?? "";
        txtMarka.Text = _kamera.Marka ?? "";
        cbAktif.Checked = _kamera.Aktif;

        int idx = _kamera.SaglayiciElleSecildi
            ? cmbSaglayici.Items.IndexOf(_kamera.Saglayici)
            : 0;
        cmbSaglayici.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void Kaydet()
    {
        _kamera.Ad = txtAd.Text.Trim();
        _kamera.Ip = txtIp.Text.Trim();
        _kamera.RtspPort = int.TryParse(txtRtspPort.Text, out var rp) ? rp : 554;
        _kamera.HttpPort = int.TryParse(txtHttpPort.Text, out var hp) ? hp : 80;
        _kamera.OnvifPort = int.TryParse(txtOnvifPort.Text, out var op) ? op : 80;
        _kamera.RtspAnaAkis = txtRtspAna.Text.NullVarsaBos();
        _kamera.RtspAltAkis = txtRtspAlt.Text.NullVarsaBos();
        _kamera.Kullanici = txtKullanici.Text.NullVarsaBos();
        _kamera.SifreSifreli = SifreKorumasi.Sifrele(txtSifre.Text);
        _kamera.Lokasyon = txtLokasyon.Text.NullVarsaBos();
        _kamera.Marka = txtMarka.Text.NullVarsaBos();
        _kamera.Aktif = cbAktif.Checked;

        if (cmbSaglayici.SelectedIndex > 0)
        {
            _kamera.SaglayiciElleSecildi = true;
            _kamera.Saglayici = cmbSaglayici.SelectedItem?.ToString() ?? "evrensel";
        }
        else
        {
            _kamera.SaglayiciElleSecildi = false;
            _kamera.Saglayici = "evrensel";
        }

        try
        {
            if (_kamera.Id == 0)
            {
                _kapsam.Kameralar.Ekle(_kamera);
            }
            else
            {
                _kapsam.Kameralar.Guncelle(_kamera);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Kaydetme hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task BaglantiTestEtAsync()
    {
        btnTest.Enabled = false;
        try
        {
            using var iptal = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var kamera = new Kamera
            {
                Ip = txtIp.Text.Trim(),
                RtspPort = int.TryParse(txtRtspPort.Text, out var rp) ? rp : 554,
                HttpPort = int.TryParse(txtHttpPort.Text, out var hp) ? hp : 80,
                OnvifPort = int.TryParse(txtOnvifPort.Text, out var op) ? op : 80,
                RtspAnaAkis = txtRtspAna.Text.NullVarsaBos(),
                Kullanici = txtKullanici.Text.NullVarsaBos(),
                SifreSifreli = SifreKorumasi.Sifrele(txtSifre.Text),
            };

            var raporlar = new List<string>();
            foreach (var saglayici in _kapsam.Secici.Tumu)
            {
                try
                {
                    var s = await saglayici.DurumKontrolAsync(kamera, iptal.Token);
                    raporlar.Add($"{saglayici.Marka,-12} {(s.Basarili ? "✓" : "✗")}  {s.Mesaj}");
                }
                catch (Exception ex)
                {
                    raporlar.Add($"{saglayici.Marka,-12} ✗  Hata: {ex.Message}");
                }
            }

            MessageBox.Show(string.Join(Environment.NewLine, raporlar), "Bağlantı testi");
        }
        finally
        {
            btnTest.Enabled = true;
        }
    }
}

internal static class MetinYardimcisi
{
    public static string? NullVarsaBos(this string metin)
    {
        string kirpilmis = metin?.Trim() ?? string.Empty;
        return kirpilmis.Length == 0 ? null : kirpilmis;
    }
}
