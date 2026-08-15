namespace KameraIzleme.Arayuz;

partial class KameraKarti
{
    private System.ComponentModel.IContainer? components = null;

    private Panel renkSeridi = null!;
    private Label etAd = null!;
    private Label etIp = null!;
    private Label etDurum = null!;
    private Label etMarka = null!;

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
        renkSeridi = new Panel();
        etAd = new Label();
        etIp = new Label();
        etDurum = new Label();
        etMarka = new Label();
        SuspendLayout();

        // renkSeridi
        renkSeridi.Dock = DockStyle.Left;
        renkSeridi.Width = 6;
        renkSeridi.BackColor = Color.Gray;

        // etAd
        etAd.Location = new Point(14, 8);
        etAd.Size = new Size(238, 22);
        etAd.Font = new Font("Segoe UI Semibold", 10F);
        etAd.AutoEllipsis = true;

        // etIp
        etIp.Location = new Point(14, 30);
        etIp.Size = new Size(238, 18);
        etIp.Font = new Font("Segoe UI", 8F);
        etIp.ForeColor = Color.DimGray;
        etIp.AutoEllipsis = true;

        // etDurum
        etDurum.Location = new Point(14, 52);
        etDurum.Size = new Size(238, 18);
        etDurum.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        etDurum.AutoEllipsis = true;

        // etMarka
        etMarka.Location = new Point(14, 72);
        etMarka.Size = new Size(238, 16);
        etMarka.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
        etMarka.ForeColor = Color.SlateGray;
        etMarka.AutoEllipsis = true;

        // KameraKarti
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.White;
        Controls.Add(etMarka);
        Controls.Add(etDurum);
        Controls.Add(etIp);
        Controls.Add(etAd);
        Controls.Add(renkSeridi);
        Margin = new Padding(4);
        Size = new Size(260, 96);
        ResumeLayout(false);
    }
}
