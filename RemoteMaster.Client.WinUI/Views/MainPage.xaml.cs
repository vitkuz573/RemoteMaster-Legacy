using Microsoft.UI.Xaml.Controls;

using RemoteMaster.Client.WinUI.ViewModels;

namespace RemoteMaster.Client.WinUI.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
