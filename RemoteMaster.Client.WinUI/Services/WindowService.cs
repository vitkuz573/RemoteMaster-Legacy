using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

using RemoteMaster.Client.WinUI.Contracts.Services;
using RemoteMaster.Client.WinUI.ViewModels;
using RemoteMaster.Client.WinUI.Views;

namespace RemoteMaster.Client.WinUI.Services;

public class WindowService : IWindowService
{
    public void OpenWindow<TViewModel>(TViewModel viewModel) where TViewModel : ObservableObject
    {
        Window newWindow;

        // Here you'd have to check for each ViewModel type and open the corresponding Window.
        // In this example, I'm assuming you only have ViewerViewModel, but you'd add more cases if you have more ViewModel types.
        if (typeof(TViewModel) == typeof(ViewerViewModel))
        {
            newWindow = new ViewerWindow();
            // Assuming ViewerWindow has a ViewModel property you can set.
            ((ViewerWindow)newWindow).ViewModel = (ViewerViewModel)(object)viewModel;
        }
        else
        {
            throw new ArgumentException("Unknown ViewModel type", nameof(viewModel));
        }

        // The rest of the initialization for the Window would go here.
        // For example, newWindow.Activate() if you want to bring the window to the front.

        newWindow.Activate(); // To bring the window to the front.
        newWindow.Show(); // To show the window.
    }
}
