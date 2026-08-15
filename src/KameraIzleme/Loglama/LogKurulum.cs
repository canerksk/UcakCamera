using Serilog;
using Serilog.Events;

namespace KameraIzleme.Loglama;

/// <summary>Serilog dosya rolling log kurulumu.</summary>
public static class LogKurulum
{
    public static void Kur()
    {
        string dizin = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(dizin);
        string dosya = Path.Combine(dizin, "kamera-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: dosya,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
