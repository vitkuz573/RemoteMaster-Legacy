using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemoteMaster.Client.WinUI.Contracts.Services;
using RemoteMaster.Client.WinUI.Views;

namespace RemoteMaster.Client.WinUI.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    private readonly IWindowService _windowService;

    public MainViewModel()
    {
        _windowService = App.GetService<IWindowService>();
    }

    [RelayCommand]
    private void OpenViewer()
    {
        _windowService.OpenWindow(new ViewerViewModel());
    }
}
