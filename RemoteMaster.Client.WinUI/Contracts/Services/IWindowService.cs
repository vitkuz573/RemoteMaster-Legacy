using CommunityToolkit.Mvvm.ComponentModel;

namespace RemoteMaster.Client.WinUI.Contracts.Services;

public interface IWindowService
{
    void OpenWindow<TViewModel>(TViewModel viewModel) where TViewModel : ObservableObject;
}
