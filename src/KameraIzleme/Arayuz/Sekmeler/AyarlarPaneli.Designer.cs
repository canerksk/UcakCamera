namespace KameraIzleme.Arayuz.Sekmeler;

partial class AyarlarPaneli
{
    private System.ComponentModel.IContainer? components = null;

    private TextBox txtHost = null!;
    private TextBox txtPort = null!;
    private CheckBox cbSsl = null!;
    private TextBox txtKullanici = null!;
    private TextBox txtSifre = null!;
    private TextBox txtGonderen = null!;
    private TextBox txtAlicilar = null!;
    private Button btnTestMail = null!;

    private TextBox txtAralik = null!;
    private TextBox txtEsik = null!;
    private TextBox txtTekrar = null!;
    private TextBox txtPingTimeout = null!;
    private TextBox txtRtspTimeout = null!;
    private TextBox txtTopluYuzde = null!;

    private CheckBox cbBaslat = null!;
    private CheckBox cbDenemeModu = null!;

    private DataGridView gridTanimlar = null!;
    private Button btnTanimEkle = null!;
    private Button btnTanimYenile = null!;

    private Button btnKaydet = null!;

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
        TextBox Tb(int x, int y, int w = 200) => new TextBox { Location = new Point(x, y), Width = w };
        CheckBox Cb(string s, int x, int y) => new CheckBox { Text = s, Location = new Point(x, y), AutoSize = true };

        var kutuMail = new GroupBox { Text = "SMTP / Mail", Location = new Point(12, 12), Size = new Size(560, 240) };
        kutuMail.Controls.AddRange(new Control[] {
            Et("Host", 12, 24), (txtHost = Tb(120, 20)),
            Et("Port", 12, 54), (txtPort = Tb(120, 50, 80)),
            (cbSsl = Cb("SSL/TLS", 220, 54)),
            Et("Kullanıcı", 12, 84), (txtKullanici = Tb(120, 80)),
            Et("Şifre", 12, 114), (txtSifre = new TextBox { Location = new Point(120, 110), Width = 200, UseSystemPasswordChar = true }),
            Et("Gönderen", 12, 144), (txtGonderen = Tb(120, 140, 300)),
            Et("Alıcılar (,)", 12, 174), (txtAlicilar = Tb(120, 170, 400)),
            (btnTestMail = new Button { Text = "Test maili gönder", Location = new Point(120, 200), Width = 200 }),
        });

        var kutuIzleme = new GroupBox { Text = "İzleme / Alarm", Location = new Point(580, 12), Size = new Size(400, 240) };
        kutuIzleme.Controls.AddRange(new Control[] {
            Et("Kontrol aralığı (sn)", 12, 24), (txtAralik = Tb(200, 20, 80)),
            Et("Ardışık hata eşiği", 12, 54), (txtEsik = Tb(200, 50, 80)),
            Et("Tekrar bildirim (dk)", 12, 84), (txtTekrar = Tb(200, 80, 80)),
            Et("Ping timeout (ms)", 12, 114), (txtPingTimeout = Tb(200, 110, 80)),
            Et("RTSP timeout (ms)", 12, 144), (txtRtspTimeout = Tb(200, 140, 80)),
            Et("Toplu kesinti % ", 12, 174), (txtTopluYuzde = Tb(200, 170, 80)),
        });

        var kutuUygulama = new GroupBox { Text = "Uygulama", Location = new Point(12, 260), Size = new Size(560, 90) };
        kutuUygulama.Controls.AddRange(new Control[] {
            (cbBaslat = Cb("Windows açılışında başlat", 12, 24)),
            (cbDenemeModu = Cb("Deneme modu (maili loga bas, sahte sunucular)", 12, 50)),
        });

        var kutuSaglayici = new GroupBox { Text = "Sağlayıcılar", Location = new Point(12, 360), Size = new Size(970, 220) };
        gridTanimlar = new DataGridView
        {
            Location = new Point(10, 22),
            Size = new Size(760, 180),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        gridTanimlar.Columns.Add("m", "Marka");
        gridTanimlar.Columns.Add("o", "Öncelik");
        gridTanimlar.Columns.Add("k", "Kullanan kamera sayısı");
        btnTanimEkle = new Button { Text = "Tanım dosyası ekle", Location = new Point(780, 24), Width = 170 };
        btnTanimYenile = new Button { Text = "Tanımları yeniden yükle", Location = new Point(780, 60), Width = 170 };
        kutuSaglayici.Controls.AddRange(new Control[] { gridTanimlar, btnTanimEkle, btnTanimYenile });

        btnKaydet = new Button { Text = "Ayarları kaydet", Location = new Point(12, 590), Width = 200, Height = 32 };

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        Controls.Add(kutuMail);
        Controls.Add(kutuIzleme);
        Controls.Add(kutuUygulama);
        Controls.Add(kutuSaglayici);
        Controls.Add(btnKaydet);
    }
}
