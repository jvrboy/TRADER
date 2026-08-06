using TraderUI.ViewModels;

namespace TraderUI.Views;

public partial class QuotesPage : ContentPage
{
    public QuotesPage(QuotesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
