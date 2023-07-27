using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;

namespace RemoteMaster.Client.Components;

public partial class AddComputerManual
{
    private bool IsOpen = false;
    public Computer _newComputer;
    public Guid? _selectedFolderId;

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Parameter]
    public ObservableCollection<Node> Nodes { get; set; }

    public AddComputerManual()
    {
        _newComputer = new Computer();
        _selectedFolderId = null;
    }

    public void Show()
    {
        IsOpen = true;
        StateHasChanged();
    }

    public void Add()
    {
        var folder = Nodes.OfType<Folder>().FirstOrDefault(f => f.NodeId == _selectedFolderId);

        if (folder != null)
        {
            _newComputer.ParentId = folder.NodeId;
            folder.Children.Add(_newComputer);
        }

        DatabaseService.AddNode(_newComputer);

        _newComputer = new Computer();
        _selectedFolderId = null;
    }

    private void OnSelectedFolderChanged(Guid? selectedId) => _selectedFolderId = selectedId;
}
