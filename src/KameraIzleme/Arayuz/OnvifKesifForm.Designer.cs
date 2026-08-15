namespace KameraIzleme.Arayuz;

partial class OnvifKesifForm
{
    private System.ComponentModel.IContainer? components = null;

    private Button btnAra = null!;
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
        btnAra = new Button { Text = "WS-Discovery ile ara", Location = new Point(6, 8), Width = 200 };
        btnEkle = new Button { Text = "Seçilenleri ekle", Location = new Point(220, 8), Width = 160 };
        ust.Controls.AddRange(new Control[] { btnAra, btnEkle });

        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        var col = new DataGridViewCheckBoxColumn { HeaderText = "Ekle", Width = 60, FillWeight = 20 };
        grid.Columns.Add(col);
        grid.Columns.Add("ip", "IP");
        grid.Columns.Add("p", "Port");
        grid.Columns.Add("u", "Üretici");
        grid.Columns.Add("m", "Model");
        grid.Columns.Add("r", "RTSP URI");

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 460);
        Text = "ONVIF Ağ Keşfi";
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(grid);
        Controls.Add(ust);
    }
}
