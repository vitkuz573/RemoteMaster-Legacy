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

    private void Create()
    {
        var folder = new Folder
        {
            Name = _name,
        };

        DatabaseService.AddNode(folder);

        DialogService.Close(true);
    }
}
