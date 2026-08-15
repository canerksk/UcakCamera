using KameraIzleme.Veri;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Serilog;

namespace KameraIzleme.Mail;

/// <summary>
/// SMTP mail gönderimi. MailKit tabanlı, deprecated <c>SmtpClient</c> kullanılmaz.
/// Ayrıca deneme modu açıkken sadece log'a yazar; gerçekten göndermez.
/// </summary>
public sealed class MailGondericisi
{
    private readonly AyarlarDeposu _ayarlar;

    public MailGondericisi(AyarlarDeposu ayarlar) => _ayarlar = ayarlar;

    public async Task<bool> GonderAsync(string konu, string govdeMetin, CancellationToken ct = default)
    {
        // Deneme modunda dış gönderimi loga bas
        if (_ayarlar.Al("uygulama.deneme_modu", false))
        {
            Log.Information("[DENEME] Mail bastırıldı — {Konu}\n{Govde}", konu, govdeMetin);
            return true;
        }

        string? host = _ayarlar.Al("mail.host");
        int port = _ayarlar.Al("mail.port", 587);
        bool ssl = _ayarlar.Al("mail.ssl", true);
        string? kullanici = _ayarlar.Al("mail.kullanici");
        string? sifreSifreli = _ayarlar.Al("mail.sifre_sifreli");
        string? gonderen = _ayarlar.Al("mail.gonderen");
        string? aliciListesi = _ayarlar.Al("mail.aliciler");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(gonderen)
            || string.IsNullOrWhiteSpace(aliciListesi))
        {
            Log.Warning("Mail ayarları eksik — gönderim atlandı: {Konu}", konu);
            return false;
        }

        var mesaj = new MimeMessage();
        mesaj.From.Add(MailboxAddress.Parse(gonderen));
        foreach (var alici in aliciListesi.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                mesaj.To.Add(MailboxAddress.Parse(alici));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Alıcı geçersiz atlanıyor: {A}", alici);
            }
        }

        mesaj.Subject = konu;
        mesaj.Body = new TextPart("plain") { Text = govdeMetin };

        try
        {
            using var istemci = new SmtpClient();
            SecureSocketOptions opt = port == 465
                ? SecureSocketOptions.SslOnConnect
                : (ssl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await istemci.ConnectAsync(host, port, opt, ct).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(kullanici))
            {
                string? sifre = SifreKorumasi.Coz(sifreSifreli);
                await istemci.AuthenticateAsync(kullanici, sifre, ct).ConfigureAwait(false);
            }

            await istemci.SendAsync(mesaj, ct).ConfigureAwait(false);
            await istemci.DisconnectAsync(true, ct).ConfigureAwait(false);
            Log.Information("Mail gönderildi: {Konu}", konu);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Mail gönderilemedi: {Konu}", konu);
            return false;
        }
    }
}
