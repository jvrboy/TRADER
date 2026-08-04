using TraderUI.ViewModels;

namespace TraderUI.Views;

public partial class AiChatPage : ContentPage
{
    private readonly AiChatViewModel _vm;

    public AiChatPage(AiChatViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        vm.Messages.CollectionChanged += (s, e) => ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (_vm.Messages.Count > 0)
            MessagesCollection.ScrollTo(_vm.Messages.Last(), animate: true);
    }
}
