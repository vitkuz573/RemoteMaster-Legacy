using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;
using System.Collections.ObjectModel;

namespace RemoteMaster.Client.Components;

public partial class AddFolder
{
    public BitModal _modalRef;
    public bool IsOpen = false;
    public Folder _newFolder;

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Parameter]
    public ObservableCollection<Node> Nodes { get; set; }

    public AddFolder()
    {
        _newFolder = new Folder();
    }

    public void Show()
    {
        IsOpen = true;
        StateHasChanged();
    }

    public void Add()
    {
        Nodes.Add(_newFolder);

        DatabaseService.AddNode(_newFolder);

        _newFolder = new Folder();
    }
}
