using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

using RemoteMaster.Client.WinUI.Contracts.Services;
using RemoteMaster.Client.WinUI.ViewModels;
using RemoteMaster.Client.WinUI.Views;

namespace RemoteMaster.Client.WinUI.Services;

public class WindowService : IWindowService
{
    public void OpenWindow<TViewModel>(TViewModel viewModel, IDictionary<string, object> parameters) where TViewModel : ObservableObject
    {
        Window newWindow;

        if (typeof(TViewModel) == typeof(ViewerViewModel))
        {
            newWindow = new ViewerWindow();
            ((ViewerWindow)newWindow).ViewModel = (ViewerViewModel)(object)viewModel;
            if (parameters != null && parameters.ContainsKey("Host"))
            {
                ((ViewerWindow)newWindow).ViewModel.Host = parameters["Host"].ToString();
            }
        }
        else
        {
            throw new ArgumentException("Unknown ViewModel type", nameof(viewModel));
        }

        newWindow.Activate();
        newWindow.Show();
    }
}