namespace KameraIzleme.Arayuz;

partial class IpTaramaForm
{
    private System.ComponentModel.IContainer? components = null;

    private TextBox txtBas = null!;
    private TextBox txtSon = null!;
    private Button btnTara = null!;
    private Button btnEkle = null!;
    private DataGridView grid = null!;

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
        txtBas = new TextBox { Text = "192.168.1.1", Location = new Point(6, 12), Width = 130 };
        txtSon = new TextBox { Text = "192.168.1.254", Location = new Point(146, 12), Width = 130 };
        btnTara = new Button { Text = "Tara", Location = new Point(290, 8), Width = 100 };
        btnEkle = new Button { Text = "Seçilenleri ekle", Location = new Point(400, 8), Width = 160 };
        ust.Controls.AddRange(new Control[] { txtBas, txtSon, btnTara, btnEkle });

        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Ekle", FillWeight = 20 });
        grid.Columns.Add("ip", "IP");
        grid.Columns.Add("p", "Açık portlar");

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(720, 460);
        Text = "IP Aralığı Tarama";
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(grid);
        Controls.Add(ust);
    }
}
