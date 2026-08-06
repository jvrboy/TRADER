using Microsoft.Extensions.DependencyInjection;

namespace TraderUI;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        // Global exception handler to prevent silent black-screen crashes
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

            // Resolve AppShell safely with fallback error page
            try
            {
                MainPage = serviceProvider.GetRequiredService<AppShell>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to resolve AppShell: {ex}");
                MainPage = new ContentPage
                {
                    Content = new ScrollView
                    {
                        Content = new VerticalStackLayout
                        {
                            Padding = 30,
                            Spacing = 15,
                            Children =
                            {
                                new Label { Text = "TRADER", FontSize = 32, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                                new Label { Text = "Initialization Error", FontSize = 20, TextColor = Colors.Orange },
                                new Label { Text = ex.Message, FontSize = 14, TextColor = Colors.LightGray },
                                new Label { Text = "Please restart the app.", FontSize = 14, TextColor = Colors.Gray }
                            }
                        }
                    }
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FATAL] App constructor failed: {ex}");
            MainPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    Padding = 30,
                    Children =
                    {
                        new Label { Text = "TRADER", FontSize = 32, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                        new Label { Text = $"Fatal: {ex.Message}", FontSize = 14, TextColor = Colors.Red }
                    }
                }
            };
        }
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
