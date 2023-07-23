using Blazorise;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;
using System.Collections.ObjectModel;

namespace RemoteMaster.Client.Components;

public partial class AddFolderModal
{
    public Modal _modalRef;
    public Folder _newFolder;
    private Validations _fluentValidations;

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Parameter]
    public ObservableCollection<Node> Nodes { get; set; }

    public AddFolderModal()
    {
        _newFolder = new Folder();
    }

    public void Show() => _modalRef.Show();

    public void Hide() => _modalRef.Hide();

    public async void AddFolder()
    {
        if (await _fluentValidations.ValidateAll())
        {
            Nodes.Add(_newFolder);

            DatabaseService.AddNode(_newFolder);

            _newFolder = new Folder();
            Hide();
        }
    }
}
