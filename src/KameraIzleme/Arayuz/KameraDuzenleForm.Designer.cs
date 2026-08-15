namespace KameraIzleme.Arayuz;

partial class KameraDuzenleForm
{
    private System.ComponentModel.IContainer? components = null;

    private TextBox txtAd = null!;
    private TextBox txtIp = null!;
    private TextBox txtRtspPort = null!;
    private TextBox txtHttpPort = null!;
    private TextBox txtOnvifPort = null!;
    private TextBox txtRtspAna = null!;
    private TextBox txtRtspAlt = null!;
    private TextBox txtKullanici = null!;
    private TextBox txtSifre = null!;
    private TextBox txtLokasyon = null!;
    private TextBox txtMarka = null!;
    private CheckBox cbAktif = null!;
    private ComboBox cmbSaglayici = null!;
    private Button btnKaydet = null!;
    private Button btnIptal = null!;
    private Button btnTest = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        Label Et(string s, int x, int y) => new Label { Text = s, Location = new Point(x, y), AutoSize = true };

        txtAd = new TextBox { Location = new Point(130, 12), Width = 250 };
        txtIp = new TextBox { Location = new Point(130, 42), Width = 200 };
        txtRtspPort = new TextBox { Location = new Point(130, 72), Width = 80 };
        txtHttpPort = new TextBox { Location = new Point(300, 72), Width = 80 };
        txtOnvifPort = new TextBox { Location = new Point(470, 72), Width = 80 };
        txtRtspAna = new TextBox { Location = new Point(130, 102), Width = 420 };
        txtRtspAlt = new TextBox { Location = new Point(130, 132), Width = 420 };
        txtKullanici = new TextBox { Location = new Point(130, 162), Width = 200 };
        txtSifre = new TextBox { Location = new Point(130, 192), Width = 200, UseSystemPasswordChar = true };
        txtLokasyon = new TextBox { Location = new Point(130, 222), Width = 420 };
        txtMarka = new TextBox { Location = new Point(130, 252), Width = 200 };
        cmbSaglayici = new ComboBox { Location = new Point(130, 282), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cbAktif = new CheckBox { Text = "Aktif", Location = new Point(130, 312), AutoSize = true };

        btnTest = new Button { Text = "Bağlantıyı test et", Location = new Point(130, 342), Width = 180 };
        btnKaydet = new Button { Text = "Kaydet", Location = new Point(330, 342), Width = 100 };
        btnIptal = new Button { Text = "İptal", Location = new Point(440, 342), Width = 100 };

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(580, 390);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        Controls.AddRange(new Control[] {
            Et("Ad", 12, 15), txtAd,
            Et("IP", 12, 45), txtIp,
            Et("RTSP portu", 12, 75), txtRtspPort,
            Et("HTTP portu", 220, 75), txtHttpPort,
            Et("ONVIF portu", 390, 75), txtOnvifPort,
            Et("RTSP ana", 12, 105), txtRtspAna,
            Et("RTSP alt", 12, 135), txtRtspAlt,
            Et("Kullanıcı", 12, 165), txtKullanici,
            Et("Şifre", 12, 195), txtSifre,
            Et("Lokasyon", 12, 225), txtLokasyon,
            Et("Marka", 12, 255), txtMarka,
            Et("Sağlayıcı", 12, 285), cmbSaglayici,
            cbAktif,
            btnTest, btnKaydet, btnIptal,
        });
    }
}
