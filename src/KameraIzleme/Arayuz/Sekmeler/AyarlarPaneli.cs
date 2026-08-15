using KameraIzleme.Veri;
using Microsoft.Win32;

namespace KameraIzleme.Arayuz.Sekmeler;

public partial class AyarlarPaneli : UserControl
{
    private readonly UygulamaKapsami _kapsam;

    public AyarlarPaneli(UygulamaKapsami kapsam)
    {
        _kapsam = kapsam;
        InitializeComponent();
        btnKaydet.Click += (_, _) => Kaydet();
        btnTestMail.Click += async (_, _) => await TestMailAsync();
        btnTanimEkle.Click += (_, _) => TanimEkle();
        btnTanimYenile.Click += (_, _) => TanimlariYenile();
        Load += (_, _) => Doldur();
    }

    private void Doldur()
    {
        var a = _kapsam.Ayarlar;
        txtHost.Text = a.Al("mail.host", "");
        txtPort.Text = a.Al("mail.port", 587).ToString();
        cbSsl.Checked = a.Al("mail.ssl", true);
        txtKullanici.Text = a.Al("mail.kullanici", "");
        txtSifre.Text = SifreKorumasi.Coz(a.Al("mail.sifre_sifreli")) ?? string.Empty;
        txtGonderen.Text = a.Al("mail.gonderen", "");
        txtAlicilar.Text = a.Al("mail.aliciler", "");

        txtAralik.Text = a.Al("izleme.aralik_saniye", 30).ToString();
        txtEsik.Text = a.Al("alarm.ardisik_hata_esigi", 3).ToString();
        txtTekrar.Text = a.Al("alarm.tekrar_bildirim_dakika", 60).ToString();
        txtPingTimeout.Text = a.Al("izleme.ping_timeout_ms", 2000).ToString();
        txtRtspTimeout.Text = a.Al("izleme.rtsp_timeout_ms", 3000).ToString();
        txtTopluYuzde.Text = a.Al("alarm.toplu_kesinti_yuzde", 50.0).ToString();

        cbBaslat.Checked = OtobaslatKurulu();
        cbDenemeModu.Checked = a.Al("uygulama.deneme_modu", false);

        TanimlariListele();
    }

    private void Kaydet()
    {
        var a = _kapsam.Ayarlar;
        a.Ata("mail.host", txtHost.Text.Trim());
        a.Ata("mail.port", int.TryParse(txtPort.Text, out var p) ? p : 587);
        a.Ata("mail.ssl", cbSsl.Checked);
        a.Ata("mail.kullanici", txtKullanici.Text.Trim());
        a.Ata("mail.sifre_sifreli", SifreKorumasi.Sifrele(txtSifre.Text));
        a.Ata("mail.gonderen", txtGonderen.Text.Trim());
        a.Ata("mail.aliciler", txtAlicilar.Text.Trim());

        a.Ata("izleme.aralik_saniye", int.TryParse(txtAralik.Text, out var s) ? s : 30);
        a.Ata("alarm.ardisik_hata_esigi", int.TryParse(txtEsik.Text, out var e) ? e : 3);
        a.Ata("alarm.tekrar_bildirim_dakika", int.TryParse(txtTekrar.Text, out var t) ? t : 60);
        a.Ata("izleme.ping_timeout_ms", int.TryParse(txtPingTimeout.Text, out var pt) ? pt : 2000);
        a.Ata("izleme.rtsp_timeout_ms", int.TryParse(txtRtspTimeout.Text, out var rt) ? rt : 3000);
        a.Ata("alarm.toplu_kesinti_yuzde", double.TryParse(txtTopluYuzde.Text, out var ty) ? ty : 50.0);
        a.Ata("uygulama.deneme_modu", cbDenemeModu.Checked);

        OtobaslatiUygula(cbBaslat.Checked);
        MessageBox.Show("Ayarlar kaydedildi.", "Ayarlar");
    }

    private async Task TestMailAsync()
    {
        btnTestMail.Enabled = false;
        try
        {
            Kaydet();
            bool ok = await _kapsam.Mail.GonderAsync(
                "[Kamera İzleme] Test maili",
                "Bu bir test mailidir. Ayarlar doğru çalışıyor.");
            MessageBox.Show(ok ? "Test maili gönderildi." : "Gönderim başarısız — logu kontrol edin.", "Test maili");
        }
        finally
        {
            btnTestMail.Enabled = true;
        }
    }

    private void TanimEkle()
    {
        using var of = new OpenFileDialog { Filter = "Sağlayıcı tanımı|*.json" };
        if (of.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            string hedef = Path.Combine(
                Saglayicilar.SaglayiciTanimYukleyici.VarsayilanDizin,
                Path.GetFileName(of.FileName));
            File.Copy(of.FileName, hedef, overwrite: true);
            TanimlariYenile();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Tanım ekleme hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void TanimlariYenile()
    {
        _kapsam.SaglayicilariYenidenYukle();
        TanimlariListele();
        MessageBox.Show(
            "Sağlayıcı tanımları diskten yeniden yüklendi. Uygulamayı yeniden başlatmak, " +
            "servisin yeni tanımları tam kullanmasını garanti eder.",
            "Sağlayıcılar");
    }

    private void TanimlariListele()
    {
        gridTanimlar.Rows.Clear();
        var kameraSayilari = _kapsam.Kameralar.Tumu()
            .GroupBy(k => k.Saglayici)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var t in _kapsam.Tanimlar)
        {
            kameraSayilari.TryGetValue(t.Marka, out int adet);
            gridTanimlar.Rows.Add(t.Marka, t.Oncelik, adet);
        }
    }

    private const string OtobaslatAnahtar = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string DegerAdi = "KameraIzleme";

    private static bool OtobaslatKurulu()
    {
        using var anahtar = Registry.CurrentUser.OpenSubKey(OtobaslatAnahtar);
        return anahtar?.GetValue(DegerAdi) is not null;
    }

    private static void OtobaslatiUygula(bool aktif)
    {
        using var anahtar = Registry.CurrentUser.OpenSubKey(OtobaslatAnahtar, writable: true);
        if (anahtar is null) return;

        if (aktif)
        {
            anahtar.SetValue(DegerAdi, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            anahtar.DeleteValue(DegerAdi, throwOnMissingValue: false);
        }
    }
}
