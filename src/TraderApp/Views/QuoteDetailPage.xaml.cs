using TraderUI.ViewModels;

namespace TraderUI.Views;

public partial class QuoteDetailPage : ContentPage
{
    private readonly QuoteDetailViewModel _vm;

    public QuoteDetailPage(QuoteDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (Shell.Current?.CurrentState?.Location?.ToString()?.Contains("symbol=") == true)
        {
            var uri = Shell.Current.CurrentState.Location.ToString();
            var symbol = System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + uri.Substring(uri.IndexOf('?'))).Query)["symbol"] ?? "EURUSD";
            await _vm.LoadAsync(symbol);
        }
    }
}
