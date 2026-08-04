using TraderUI.Views;

namespace TraderUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for detail pages
        Routing.RegisterRoute(nameof(QuoteDetailPage), typeof(QuoteDetailPage));
        Routing.RegisterRoute(nameof(SignalDetailPage), typeof(SignalDetailPage));
        Routing.RegisterRoute(nameof(BotTradeDetailPage), typeof(BotTradeDetailPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(ChartDetailPage), typeof(ChartDetailPage));
    }
}
