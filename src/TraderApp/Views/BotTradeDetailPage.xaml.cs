using TraderUI.ViewModels;

namespace TraderUI.Views;

public partial class BotTradeDetailPage : ContentPage
{
    public BotTradeDetailPage(BotTradeDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
