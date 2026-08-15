namespace KameraIzleme.Arayuz;

partial class AnaForm
{
    private System.ComponentModel.IContainer? components = null;

    private TabControl sekmeler = null!;
    private TabPage sekmeCanli = null!;
    private TabPage sekmeKameralar = null!;
    private TabPage sekmeOlaylar = null!;
    private TabPage sekmeAyarlar = null!;

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

        sekmeler = new TabControl();
        sekmeCanli = new TabPage("Canlı Durum");
        sekmeKameralar = new TabPage("Kameralar");
        sekmeOlaylar = new TabPage("Olaylar ve İstatistik");
        sekmeAyarlar = new TabPage("Ayarlar");

        sekmeler.Dock = DockStyle.Fill;
        sekmeler.TabPages.Add(sekmeCanli);
        sekmeler.TabPages.Add(sekmeKameralar);
        sekmeler.TabPages.Add(sekmeOlaylar);
        sekmeler.TabPages.Add(sekmeAyarlar);

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1280, 800);
        Controls.Add(sekmeler);
        Font = new Font("Segoe UI", 9F);
        Name = "AnaForm";
        Text = "Kamera İzleme";
    }
}
