using BeautyByNegin.Business.Chat;
using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Inbox;
using BeautyByNegin.Business.Notifications;
using BeautyByNegin.Business.Localization;
using BeautyByNegin.Business.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace BeautyByNegin.Business;

public static class BusinessSetup
{
    /// <summary>Registers all business-layer services.</summary>
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ILanguageService, LanguageService>();
        services.AddSingleton<ITextService, TextService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILayoutService, LayoutService>();
        services.AddSingleton<IContentService, ContentService>();
        services.AddSingleton<ISiteCache, SiteCache>();

        // Notifications (e-mail + Telegram), delivered in the background
        services.AddHttpClient(TelegramNotifier.HttpClientName, c =>
        {
            c.BaseAddress = new Uri("https://api.telegram.org/");
            c.Timeout = TimeSpan.FromSeconds(12);
        });
        services.AddSingleton<ITelegramNotifier, TelegramNotifier>();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<NotificationQueue>();
        services.AddSingleton<INotificationQueue>(sp => sp.GetRequiredService<NotificationQueue>());
        services.AddHostedService<NotificationWorker>();

        // Visitor submissions and chat
        services.AddScoped<IInboxService, InboxService>();
        services.AddScoped<IChatService, ChatService>();
        return services;
    }
}
