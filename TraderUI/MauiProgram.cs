using Microsoft.Extensions.Logging;
using TraderUI.Services;
using TraderUI.ViewModels;
using TraderUI.Views;

namespace TraderUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
            });

        // ==================== SERVICES ====================
        // Core Services
        builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();

        // AI & Analysis Services (Backend Tools)
        builder.Services.AddSingleton<IAiChatService, AiChatService>();
        builder.Services.AddSingleton<ISignalService, SignalService>();
        builder.Services.AddSingleton<IBotService, BotService>();
        builder.Services.AddSingleton<IIndicatorService, IndicatorService>();
        builder.Services.AddSingleton<IDivergenceService, DivergenceService>();
        builder.Services.AddSingleton<IDriftLabService, DriftLabService>();
        builder.Services.AddSingleton<ISyntheticsService, SyntheticsService>();
        builder.Services.AddSingleton<IAiBrainService, AiBrainService>();
        builder.Services.AddSingleton<IChartAnalysisService, ChartAnalysisService>();
        builder.Services.AddSingleton<ISwarmAnalysisService, SwarmAnalysisService>();
        builder.Services.AddSingleton<IDerivApiService, DerivApiService>();

        // ==================== VIEW MODELS ====================
        builder.Services.AddSingleton<AiChatViewModel>();
        builder.Services.AddSingleton<QuotesViewModel>();
        builder.Services.AddSingleton<ChartViewModel>();
        builder.Services.AddSingleton<SignalsViewModel>();
        builder.Services.AddSingleton<BotViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<QuoteDetailViewModel>();
        builder.Services.AddTransient<SignalDetailViewModel>();
        builder.Services.AddTransient<BotTradeDetailViewModel>();

        // ==================== VIEWS ====================
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<AiChatPage>();
        builder.Services.AddSingleton<QuotesPage>();
        builder.Services.AddSingleton<ChartPage>();
        builder.Services.AddSingleton<SignalsPage>();
        builder.Services.AddSingleton<BotPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<QuoteDetailPage>();
        builder.Services.AddTransient<SignalDetailPage>();
        builder.Services.AddTransient<BotTradeDetailPage>();
        builder.Services.AddTransient<ChartDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
