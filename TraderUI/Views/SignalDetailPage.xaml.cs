using TraderUI.ViewModels;

namespace TraderUI.Views;

public partial class SignalDetailPage : ContentPage
{
    public SignalDetailPage(SignalDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
