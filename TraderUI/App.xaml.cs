namespace TraderUI;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        MainPage = serviceProvider.GetRequiredService<AppShell>();
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
