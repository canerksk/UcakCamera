using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using KameraIzleme.Modeller;

namespace KameraIzleme.Arayuz;

/// <summary>Belirtilen IP aralığında 554/80/8000 portu açık cihazları tarar.</summary>
public partial class IpTaramaForm : Form
{
    private readonly UygulamaKapsami _kapsam;
    private readonly List<TaramaBulgusu> _bulgular = new();

    public IpTaramaForm(UygulamaKapsami kapsam)
    {
        _kapsam = kapsam;
        InitializeComponent();
        btnTara.Click += async (_, _) => await TaraAsync();
        btnEkle.Click += (_, _) => Ekle();
    }

    private async Task TaraAsync()
    {
        btnTara.Enabled = false;
        grid.Rows.Clear();
        _bulgular.Clear();

        var ilkParcalar = txtBas.Text.Split('.');
        var sonParcalar = txtSon.Text.Split('.');
        if (ilkParcalar.Length != 4 || sonParcalar.Length != 4)
        {
            MessageBox.Show("Geçerli IP aralığı gir.", "Tarama");
            btnTara.Enabled = true;
            return;
        }

        int a = int.Parse(ilkParcalar[3]);
        int b = int.Parse(sonParcalar[3]);
        string onEk = string.Join('.', ilkParcalar.Take(3));

        var portlar = new[] { 554, 80, 8000 };
        var gorevler = new List<Task>();
        using var semafor = new SemaphoreSlim(40);

        for (int i = a; i <= b; i++)
        {
            string ip = $"{onEk}.{i}";
            await semafor.WaitAsync();
            gorevler.Add(Task.Run(async () =>
            {
                try
                {
                    if (await PingAtAsync(ip))
                    {
                        var acikPortlar = new List<int>();
                        foreach (int p in portlar)
                        {
                            if (await PortAcikMi(ip, p))
                            {
                                acikPortlar.Add(p);
                            }
                        }

                        if (acikPortlar.Count > 0)
                        {
                            var bulgu = new TaramaBulgusu { Ip = ip, Portlar = acikPortlar };
                            lock (_bulgular)
                            {
                                _bulgular.Add(bulgu);
                            }

                            BeginInvoke(() => grid.Rows.Add(false, ip, string.Join(", ", acikPortlar)));
                        }
                    }
                }
                finally
                {
                    semafor.Release();
                }
            }));
        }

        await Task.WhenAll(gorevler);
        btnTara.Enabled = true;
        MessageBox.Show($"Tarama bitti — {_bulgular.Count} aday.", "Tarama");
    }

    private static async Task<bool> PingAtAsync(string ip)
    {
        try
        {
            using var p = new Ping();
            var r = await p.SendPingAsync(ip, 1000);
            return r.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> PortAcikMi(string ip, int port)
    {
        try
        {
            using var tcp = new TcpClient();
            var baglan = tcp.ConnectAsync(ip, port);
            var tamamlanan = await Task.WhenAny(baglan, Task.Delay(600));
            return tamamlanan == baglan && tcp.Connected;
        }
        catch
        {
            return false;
        }
    }

    private void Ekle()
    {
        int eklendi = 0;
        for (int i = 0; i < grid.Rows.Count; i++)
        {
            if (grid.Rows[i].Cells[0].Value is not true) continue;
            var b = _bulgular[i];
            if (_kapsam.Kameralar.IpIleGetir(b.Ip) is not null) continue;

            _kapsam.Kameralar.Ekle(new Kamera
            {
                Ad = b.Ip,
                Ip = b.Ip,
                Saglayici = "evrensel",
            });
            eklendi++;
        }

        MessageBox.Show($"{eklendi} kamera eklendi.", "IP Tarama");
        if (eklendi > 0)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private sealed class TaramaBulgusu
    {
        public string Ip { get; set; } = "";
        public List<int> Portlar { get; set; } = new();
    }
}
