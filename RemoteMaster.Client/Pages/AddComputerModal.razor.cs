using Blazorise;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;
using System.Collections.ObjectModel;

namespace RemoteMaster.Client.Components;

public partial class AddComputerModal
{
    public Modal _modalRef;
    public Computer _newComputer;
    public Guid? _selectedFolderId;
    private Validations _fluentValidations;

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Parameter]
    public ObservableCollection<Node> Nodes { get; set; }

    public AddComputerModal()
    {
        _newComputer = new Computer();
        _selectedFolderId = null;
    }

    public void Show() => _modalRef.Show();

    public void Hide() => _modalRef.Hide();

    public async void AddComputer()
    {
        if (await _fluentValidations.ValidateAll())
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
            Hide();
        }
    }

    private void OnSelectedFolderChanged(Guid? selectedId) => _selectedFolderId = selectedId;
}
