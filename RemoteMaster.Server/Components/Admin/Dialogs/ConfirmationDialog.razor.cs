using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Admin.Dialogs;

public partial class ConfirmationDialog
{
    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public string Message { get; set; }

    [Parameter]
    public EventCallback<bool> OnConfirm { get; set; }

    private bool _isVisible;

    public void Show()
    {
        _isVisible = true;

        StateHasChanged();
    }

    private void Confirm(bool confirmed)
    {
        _isVisible = false;
        OnConfirm.InvokeAsync(confirmed);
    }
}
