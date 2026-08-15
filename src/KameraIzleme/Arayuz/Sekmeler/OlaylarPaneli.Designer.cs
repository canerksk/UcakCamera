using ScottPlot.WinForms;

namespace KameraIzleme.Arayuz.Sekmeler;

partial class OlaylarPaneli
{
    private System.ComponentModel.IContainer? components = null;

    private DateTimePicker dtBas = null!;
    private DateTimePicker dtBit = null!;
    private ComboBox cmbKamera = null!;
    private Button btnUygula = null!;
    private Button btnCsv = null!;

    private DataGridView gridOlaylar = null!;
    private DataGridView gridUptime = null!;
    private DataGridView gridSorunlu = null!;
    private FormsPlot grafik = null!;

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
        dtBas = new DateTimePicker { Location = new Point(6, 10), Width = 200, Value = DateTime.Now.AddDays(-7) };
        dtBit = new DateTimePicker { Location = new Point(212, 10), Width = 200, Value = DateTime.Now.AddDays(1) };
        cmbKamera = new ComboBox { Location = new Point(420, 10), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        btnUygula = new Button { Location = new Point(630, 8), Width = 100, Text = "Uygula" };
        btnCsv = new Button { Location = new Point(736, 8), Width = 120, Text = "CSV Dışa Aktar" };
        ust.Controls.AddRange(new Control[] { dtBas, dtBit, cmbKamera, btnUygula, btnCsv });

        gridOlaylar = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        gridOlaylar.Columns.Add("bas", "Başladı");
        gridOlaylar.Columns.Add("kam", "Kamera");
        gridOlaylar.Columns.Add("tip", "Tip");
        gridOlaylar.Columns.Add("kay", "Kaynak");
        gridOlaylar.Columns.Add("bit", "Bitti");
        gridOlaylar.Columns.Add("sur", "Süre (sn)");
        gridOlaylar.Columns.Add("mes", "Mesaj");

        // Alt bölme: uptime + sorunlu + grafik
        var alt = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 260,
            ColumnCount = 3,
            RowCount = 1,
        };
        alt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        alt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        alt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        gridUptime = new DataGridView
        {
            Dock = DockStyle.Fill,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        gridUptime.Columns.Add("k", "Kamera");
        gridUptime.Columns.Add("24s", "24 sa");
        gridUptime.Columns.Add("7g", "7 gün");
        gridUptime.Columns.Add("30g", "30 gün");

        gridSorunlu = new DataGridView
        {
            Dock = DockStyle.Fill,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        gridSorunlu.Columns.Add("k", "En sorunlu kamera");
        gridSorunlu.Columns.Add("s", "Kesinti sayısı");

        grafik = new FormsPlot { Dock = DockStyle.Fill };

        alt.Controls.Add(gridUptime, 0, 0);
        alt.Controls.Add(gridSorunlu, 1, 0);
        alt.Controls.Add(grafik, 2, 0);

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(gridOlaylar);
        Controls.Add(alt);
        Controls.Add(ust);
    }
}
