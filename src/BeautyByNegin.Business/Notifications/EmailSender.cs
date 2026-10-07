using Microsoft.Extensions.Hosting;
using BeautyByNegin.Business.Settings;
using BeautyByNegin.DataAccess;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BeautyByNegin.Business.Notifications;

public sealed record SendResult(bool Ok, string? Error = null)
{
    public static readonly SendResult Success = new(true);
    public static SendResult Fail(string error) => new(false, error);
}

public interface IEmailSender
{
    /// <summary>Sends a plain-text e-mail using the SMTP settings from the panel.</summary>
    Task<SendResult> SendAsync(string to, string subject, string body, string? replyTo = null, CancellationToken ct = default);
}

/// <summary>
/// SMTP via MailKit (supports STARTTLS on 587 and implicit TLS on 465).
/// Foreign SMTP servers may be unreachable from Iran, so callers always save to the database first.
/// </summary>
public sealed class EmailSender(ISettingsService settings, ILogger<EmailSender> logger, IHostEnvironment env) : IEmailSender
{
    public async Task<SendResult> SendAsync(string to, string subject, string body, string? replyTo = null, CancellationToken ct = default)
    {
        var s = await settings.GetAsync(ct);
        if (!s.SmtpConfigured)
        {
            if (!s.DevMailToFile) return SendResult.Fail("smtp-not-configured");
            // Local testing without SMTP: keep the e-mail as a text file and show it in the log (Visual Studio "Output").
            var dir = Path.Combine(AppPaths.AppData(env.ContentRootPath), "dev-mail");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}.txt");
            await File.WriteAllTextAsync(file, $"To: {to}\nSubject: {subject}\n\n{body}\n", ct);
            logger.LogWarning("DEV MAIL (not sent, SMTP not configured) to {To}: {Subject}\n{Body}", to, subject, body);
            return SendResult.Success;
        }

        var host = s.Text(SettingKeys.SmtpHost);
        var port = s.Int(SettingKeys.SmtpPort) ?? 587;
        var user = s.Text(SettingKeys.SmtpUser);
        var password = await settings.GetSecretAsync(SettingKeys.SmtpPassword, ct);
        var from = s.Text(SettingKeys.SmtpFromAddress, user);
        if (string.IsNullOrWhiteSpace(from)) return SendResult.Fail("smtp-no-sender");

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(s.Text(SettingKeys.SmtpFromName, s.BrandName), from));
            message.To.Add(MailboxAddress.Parse(to));
            if (!string.IsNullOrWhiteSpace(replyTo) && MailboxAddress.TryParse(replyTo, out var reply))
                message.ReplyTo.Add(reply);
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient { Timeout = 20000 };
            var security = port == 465 ? SecureSocketOptions.SslOnConnect
                : s.Bool(SettingKeys.SmtpUseSsl) ? SecureSocketOptions.StartTls
                : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(host, port, security, ct);
            if (!string.IsNullOrWhiteSpace(user))
                await client.AuthenticateAsync(user, password ?? "", ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            return SendResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "E-mail to {To} failed", to);
            return SendResult.Fail(ex.Message);
        }
    }
}
