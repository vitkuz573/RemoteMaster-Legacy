using CommunityToolkit.Mvvm.ComponentModel;

namespace RemoteMaster.Client.WinUI.Contracts.Services;

public interface IWindowService
{
    void OpenWindow<TViewModel>(IDictionary<string, object> parameters) where TViewModel : ObservableObject;
}
