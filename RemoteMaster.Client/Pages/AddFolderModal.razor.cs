using Blazorise;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Pages;

public partial class AddFolderModal
{
    private Modal _modalRef;
    private Folder _newFolder = new();

    private Validations _fluentValidations;

    public void Show()
    {
        _modalRef.Show();
    }

    public void Hide()
    {
        _modalRef.Hide();
    }

    public async void AddFolder()
    {
        if (await _fluentValidations.ValidateAll())
        {
            await OnAdd.InvokeAsync(_newFolder);
            _newFolder = new Folder();
            Hide();
        }
    }

    [Parameter]
    public EventCallback<Folder> OnAdd { get; set; }
}
