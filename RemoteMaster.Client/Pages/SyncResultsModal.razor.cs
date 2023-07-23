using Blazorise;
using RemoteMaster.Client.Models;
using System.Collections.ObjectModel;

namespace RemoteMaster.Client.Components;

public partial class SyncResultsModal
{
    private Modal _modalRef;
    private ObservableCollection<Node> _adNodes = new();
    private IList<Node> _expandedNodes = new List<Node>();
    private Node _selectedNode;

    public void Show(ObservableCollection<Node> adNodes)
    {
        _adNodes = adNodes;
        _modalRef.Show();
    }

    public void Hide()
    {
        _modalRef.Hide();
    }
}
