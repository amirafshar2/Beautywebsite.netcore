using BeautyByNegin.Business.Admin;
using BeautyByNegin.Business.Chat;
using BeautyByNegin.Business.Media;
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

        // Automatic translation in the panel (Google Gemini)
        services.AddHttpClient(Ai.GeminiTranslator.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(90));
        services.AddSingleton<Ai.IAiTranslator, Ai.GeminiTranslator>();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<NotificationQueue>();
        services.AddSingleton<INotificationQueue>(sp => sp.GetRequiredService<NotificationQueue>());
        services.AddHostedService<NotificationWorker>();

        // Visitor submissions and chat
        services.AddScoped<IInboxService, InboxService>();
        services.AddScoped<Customers.ICustomerAccountService, Customers.CustomerAccountService>();
        services.AddScoped<IChatService, ChatService>();

        // Admin panel
        services.AddScoped<IMediaService, MediaService>();
        services.AddSingleton<VideoQueue>();
        services.AddSingleton<FfmpegLocator>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddHostedService<VideoWorker>();
        services.AddScoped<Content.SampleContentSeeder>();
        services.AddScoped<IAdminData, AdminData>();
        services.AddScoped<ITrashService, TrashService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IAdminInboxService, AdminInboxService>();
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IAdminTextService, AdminTextService>();
        services.AddHostedService<MaintenanceWorker>();
        return services;
    }
}
