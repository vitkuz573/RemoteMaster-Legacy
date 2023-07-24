using System.ComponentModel;
using Microsoft.UI.Xaml;
using RemoteMaster.Client.WinUI.ViewModels;

namespace RemoteMaster.Client.WinUI.Views;

public sealed partial class ViewerWindow : Window
{
    public ViewerViewModel ViewModel
    {
        get;
        set;
    }

    public ViewerWindow()
    {
        ViewModel = App.GetService<ViewerViewModel>();
        InitializeComponent();
        Closed += OnViewerWindowClosed;
    }

    private void OnViewerWindowClosed(object sender, WindowEventArgs e)
    {
        ViewModel.Dispose();
    }
}
