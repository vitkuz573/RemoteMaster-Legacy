// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Radzen;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Pages;

public partial class NewComputerPage
{
    [Inject]
    private DialogService DialogService
    {
        get; set;
    }

    [Inject]
    private DatabaseService DatabaseService
    {
        get; set;
    }

    private string _name;
    private string _ip;
    private Guid _selectedFolderId;
    private IList<Folder> _folders;

    protected override void OnInitialized()
    {
        _folders = DatabaseService.GetFolders();
    }

    private void Create()
    {
        var folder = _folders.FirstOrDefault(f => f.NodeId == _selectedFolderId);

        var computer = new Computer
        {
            Name = _name,
            IPAddress = _ip,
            Parent = folder,
        };

        DatabaseService.AddNode(computer);

        DialogService.Close(true);
    }
}
