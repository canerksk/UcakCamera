namespace KameraIzleme.Arayuz.Sekmeler;

partial class CanliDurumPaneli
{
    private System.ComponentModel.IContainer? components = null;

    private Label etOzet = null!;
    private TextBox txtArama = null!;
    private ComboBox cmbFiltre = null!;
    private ComboBox cmbMarka = null!;
    private ComboBox cmbKatman = null!;
    private Button btnSimdi = null!;
    private FlowLayoutPanel panelKartlar = null!;

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

        var ust = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(8) };

        etOzet = new Label
        {
            Location = new Point(10, 10),
            Size = new Size(1000, 20),
            Font = new Font("Segoe UI Semibold", 10F),
        };

        txtArama = new TextBox { Location = new Point(10, 36), Size = new Size(200, 24), PlaceholderText = "Ara (ad/IP)" };
        cmbFiltre = new ComboBox { Location = new Point(220, 36), Size = new Size(180, 24), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbMarka = new ComboBox { Location = new Point(410, 36), Size = new Size(160, 24), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbKatman = new ComboBox { Location = new Point(580, 36), Size = new Size(160, 24), DropDownStyle = ComboBoxStyle.DropDownList };
        btnSimdi = new Button { Location = new Point(750, 34), Size = new Size(140, 28), Text = "Şimdi Kontrol Et" };

        ust.Controls.Add(etOzet);
        ust.Controls.Add(txtArama);
        ust.Controls.Add(cmbFiltre);
        ust.Controls.Add(cmbMarka);
        ust.Controls.Add(cmbKatman);
        ust.Controls.Add(btnSimdi);

        panelKartlar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(245, 246, 248),
        };

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(panelKartlar);
        Controls.Add(ust);
    }
}
