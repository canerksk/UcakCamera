namespace KameraIzleme.Arayuz.Sekmeler;

partial class KameralarPaneli
{
    private System.ComponentModel.IContainer? components = null;

    private DataGridView gridKameralar = null!;
    private Button btnYeni = null!;
    private Button btnDuzenle = null!;
    private Button btnSil = null!;
    private Button btnAktif = null!;
    private Button btnKesif = null!;
    private Button btnTara = null!;
    private Button btnCsv = null!;

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

        var ust = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(6) };
        btnYeni = new Button { Text = "Yeni", Width = 90, Location = new Point(6, 8) };
        btnDuzenle = new Button { Text = "Düzenle", Width = 90, Location = new Point(100, 8) };
        btnSil = new Button { Text = "Sil", Width = 70, Location = new Point(194, 8) };
        btnAktif = new Button { Text = "Aktif/Pasif", Width = 100, Location = new Point(268, 8) };
        btnKesif = new Button { Text = "ONVIF Keşif", Width = 120, Location = new Point(390, 8) };
        btnTara = new Button { Text = "IP Aralığı Tara", Width = 140, Location = new Point(516, 8) };
        btnCsv = new Button { Text = "CSV İçe Aktar", Width = 130, Location = new Point(662, 8) };
        ust.Controls.AddRange(new Control[] { btnYeni, btnDuzenle, btnSil, btnAktif, btnKesif, btnTara, btnCsv });

        gridKameralar = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
        };

        gridKameralar.Columns.Add("id", "Id");
        gridKameralar.Columns.Add("ad", "Ad");
        gridKameralar.Columns.Add("ip", "IP");
        gridKameralar.Columns.Add("marka", "Marka");
        gridKameralar.Columns.Add("sag", "Sağlayıcı");
        gridKameralar.Columns.Add("lok", "Lokasyon");
        gridKameralar.Columns.Add("aktif", "Aktif");
        gridKameralar.Columns[0].Visible = false;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(gridKameralar);
        Controls.Add(ust);
    }
}
