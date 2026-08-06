using Microsoft.Extensions.DependencyInjection;

namespace TraderUI;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        // Global exception handlers - must be set BEFORE any MAUI init
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[FATAL] UnhandledException: {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"[FATAL] UnobservedTaskException: {e.Exception}");
            e.SetObserved();
        };

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FATAL] InitializeComponent failed: {ex}");
            MainPage = MakeErrorPage("XAML Init Error", ex.Message);
            return;
        }

        try
        {
            MainPage = serviceProvider.GetRequiredService<AppShell>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] AppShell resolution failed: {ex}");
            // Inner exception is the real cause
            var inner = ex.InnerException ?? ex;
            var msg = inner.GetType().Name + ": " + inner.Message;
            if (inner.StackTrace != null)
                msg += "\n" + inner.StackTrace.Split('\n').Take(5);
            MainPage = MakeErrorPage("Startup Error", msg);
        }
    }

    /// <summary>
    /// Creates a minimal error page using NO static resources, NO converters, NO styles.
    /// This guarantees visibility even if the resource system is broken.
    /// </summary>
    private static ContentPage MakeErrorPage(string title, string message)
    {
        return new ContentPage
        {
            BackgroundColor = Color.FromArgb("#0A0E1A"),
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = 24,
                    Spacing = 12,
                    Children =
                    {
                        new Label { Text = "TRADER", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = title, FontSize = 18, TextColor = Colors.OrangeRed, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0,8,0,8) },
                        new Frame { BackgroundColor = Color.FromArgb("#1A1A2E"), Padding = 16, Content = new Label { Text = message, FontSize = 13, TextColor = Colors.LightGray, LineBreakMode = LineBreakMode.WordWrap } },
                        new Label { Text = "Restart the app to retry.", FontSize = 13, TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0,12,0,0) }
                    }
                }
            }
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Title = "TRADER";
        window.MinimumWidth = 400;
        window.MinimumHeight = 700;
        return window;
    }
}
