using TraderUI.ViewModels;

namespace TraderUI.Views;

public partial class BotPage : ContentPage
{
    public BotPage(BotViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
