using System.Runtime.InteropServices;
using KameraIzleme.Arayuz;
using KameraIzleme.Loglama;
using KameraIzleme.Veri;
using Serilog;

namespace KameraIzleme;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        LogKurulum.Kur();
        Log.Information("Kamera İzleme başlatılıyor");

        try
        {
            ApplicationConfiguration.Initialize();

            // Veritabanı ilk açılışta şemayı kurar
            VeriTabani.SemayiKur();

            Application.Run(new AnaForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Uygulama beklenmedik şekilde sonlandı");
            MessageBox.Show(
                $"Beklenmeyen hata:\n{ex.Message}",
                "Kamera İzleme",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
