using CommunityToolkit.Mvvm.ComponentModel;

namespace RemoteMaster.Client.WinUI.ViewModels;

public partial class ViewerViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _imageUrl;
}
