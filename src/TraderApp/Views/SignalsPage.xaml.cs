using TraderUI.ViewModels;

namespace TraderUI.Views;

public partial class SignalsPage : ContentPage
{
    public SignalsPage(SignalsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
