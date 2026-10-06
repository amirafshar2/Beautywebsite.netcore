using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace BeautyByNegin.Business.Notifications;

/// <summary>A notification to deliver in the background (so a slow/blocked Telegram or SMTP never delays a visitor).</summary>
public sealed record Notification(
    string Kind,
    string TelegramHtml,
    string EmailSubject,
    string EmailBody,
    string? ReplyTo = null,
    Func<IServiceProvider, bool, Task>? OnTelegramResult = null);

public interface INotificationQueue
{
    void Enqueue(Notification notification);
}

public sealed class NotificationQueue : INotificationQueue
{
    private readonly Channel<Notification> _channel = Channel.CreateBounded<Notification>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest });

    public ChannelReader<Notification> Reader => _channel.Reader;

    public void Enqueue(Notification notification) => _channel.Writer.TryWrite(notification);
}

/// <summary>Delivers queued notifications to Telegram and/or the notification e-mail address.</summary>
public sealed class NotificationWorker(
    NotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var n in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;
                var settings = await sp.GetRequiredService<Settings.ISettingsService>().GetAsync(stoppingToken);

                var telegramFlag = n.Kind switch
                {
                    NotificationKinds.Appointment => DataAccess.SettingKeys.TelegramNotifyAppointments,
                    NotificationKinds.Message => DataAccess.SettingKeys.TelegramNotifyMessages,
                    NotificationKinds.Chat => DataAccess.SettingKeys.TelegramNotifyChat,
                    NotificationKinds.Newsletter => DataAccess.SettingKeys.TelegramNotifyNewsletter,
                    _ => null
                };
                var telegramOk = false;
                if (telegramFlag is null || settings.Bool(telegramFlag))
                {
                    var r = await sp.GetRequiredService<ITelegramNotifier>().SendAsync(n.TelegramHtml, stoppingToken);
                    telegramOk = r.Ok;
                }
                if (n.OnTelegramResult is not null) await n.OnTelegramResult(sp, telegramOk);

                // E-mail notification (optional; reviews/newsletter only via Telegram to avoid noise).
                var notifyEmail = settings.Text(DataAccess.SettingKeys.NotificationEmail);
                if (!string.IsNullOrWhiteSpace(notifyEmail) && settings.SmtpConfigured
                    && n.Kind is NotificationKinds.Appointment or NotificationKinds.Message or NotificationKinds.Chat)
                {
                    await sp.GetRequiredService<IEmailSender>().SendAsync(notifyEmail, n.EmailSubject, n.EmailBody, n.ReplyTo, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Notification {Kind} failed", n.Kind);
            }
        }
    }
}

public static class NotificationKinds
{
    public const string Appointment = "appointment";
    public const string Message = "message";
    public const string Chat = "chat";
    public const string Newsletter = "newsletter";
    public const string Review = "review";
}
