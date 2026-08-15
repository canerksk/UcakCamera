using System.Globalization;
using KameraIzleme.Modeller;

namespace KameraIzleme.Arayuz.Sekmeler;

public partial class KameralarPaneli : UserControl
{
    private readonly UygulamaKapsami _kapsam;

    public KameralarPaneli(UygulamaKapsami kapsam)
    {
        _kapsam = kapsam;
        InitializeComponent();
        btnYeni.Click += (_, _) => Duzenle(null);
        btnDuzenle.Click += (_, _) => SecilenIcin(k => Duzenle(k));
        btnSil.Click += (_, _) => SecilenIcin(SilOnayla);
        btnAktif.Click += (_, _) => SecilenIcin(AktifligiCevir);
        btnKesif.Click += (_, _) => new OnvifKesifForm(_kapsam).ShowDialog(this);
        btnTara.Click += (_, _) => new IpTaramaForm(_kapsam).ShowDialog(this);
        btnCsv.Click += (_, _) => CsvIceAktar();
        Load += (_, _) => Yenile();
    }

    private void Yenile()
    {
        gridKameralar.SuspendLayout();
        gridKameralar.Rows.Clear();
        foreach (var k in _kapsam.Kameralar.Tumu())
        {
            gridKameralar.Rows.Add(k.Id, k.Ad, k.Ip, k.Marka, k.Saglayici,
                k.Lokasyon, k.Aktif ? "Evet" : "Hayır");
        }

        gridKameralar.ResumeLayout();
    }

    private void SecilenIcin(Action<Kamera> islem)
    {
        if (gridKameralar.CurrentRow is null) return;
        long id = (long)gridKameralar.CurrentRow.Cells[0].Value;
        var k = _kapsam.Kameralar.Getir(id);
        if (k is not null)
        {
            islem(k);
        }
    }

    private void Duzenle(Kamera? mevcut)
    {
        using var form = new KameraDuzenleForm(_kapsam, mevcut);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            Yenile();
        }
    }

    private void SilOnayla(Kamera k)
    {
        if (MessageBox.Show($"{k.Ad} silinsin mi?", "Onay", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            _kapsam.Kameralar.Sil(k.Id);
            Yenile();
        }
    }

    private void AktifligiCevir(Kamera k)
    {
        k.Aktif = !k.Aktif;
        _kapsam.Kameralar.Guncelle(k);
        Yenile();
    }

    private void CsvIceAktar()
    {
        using var of = new OpenFileDialog { Filter = "CSV|*.csv" };
        if (of.ShowDialog(this) != DialogResult.OK) return;

        int eklendi = 0;
        try
        {
            foreach (var satir in File.ReadAllLines(of.FileName).Skip(1))
            {
                var parcalar = satir.Split(',');
                if (parcalar.Length < 3) continue;
                var k = new Kamera
                {
                    Ad = parcalar[0].Trim(),
                    Ip = parcalar[1].Trim(),
                    Lokasyon = parcalar.Length > 2 ? parcalar[2].Trim() : null,
                    Marka = parcalar.Length > 3 ? parcalar[3].Trim() : null,
                    Kullanici = parcalar.Length > 4 ? parcalar[4].Trim() : null,
                    SifreSifreli = parcalar.Length > 5 ? Veri.SifreKorumasi.Sifrele(parcalar[5].Trim()) : null,
                };
                _kapsam.Kameralar.Ekle(k);
                eklendi++;
            }

            MessageBox.Show($"{eklendi} kamera eklendi.", "İçe aktar");
            Yenile();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "İçe aktar hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
