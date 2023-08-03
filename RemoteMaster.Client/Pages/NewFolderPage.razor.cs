// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using Microsoft.AspNetCore.Components;
using Radzen;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;

namespace RemoteMaster.Client.Pages;

public partial class NewFolderPage
{
    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    private string _name;
    private Guid _selectedParentId;
    private IList<Folder> _folders;

    protected override void OnInitialized()
    {
        _folders = DatabaseService.GetFolders();
    }

    private void Create()
    {
        var parentFolder = _folders.FirstOrDefault(f => f.NodeId == _selectedParentId);

        var folder = new Folder
        {
            Name = _name,
            Parent = parentFolder
        };

        DatabaseService.AddNode(folder);

        DialogService.Close(true);
    }
}
