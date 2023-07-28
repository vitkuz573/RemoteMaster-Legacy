using System.Collections.ObjectModel;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Components;

public partial class AddComputerFromAD
{
    // private BitModal _modalRef;
    private bool IsOpen = false;
    private ObservableCollection<Node> _adNodes = new();
    private IList<Node> _expandedNodes = new List<Node>();
    private Node _selectedNode;

    public void Show(ObservableCollection<Node> adNodes)
    {
        _adNodes = adNodes;
        IsOpen = true;
        StateHasChanged();
    }
}
