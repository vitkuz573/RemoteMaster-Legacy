using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Admin.Dialogs;

public partial class ConfirmationDialog
{
    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public string Message { get; set; }

#pragma warning disable CA2227
    [Parameter]
    public Dictionary<string, string> Parameters { get; set; }
#pragma warning restore CA2227

    [Parameter]
    public EventCallback<bool> OnConfirm { get; set; }

    private bool _isVisible;

    public void Show(Dictionary<string, string> parameters)
    {
        Parameters = parameters;
        _isVisible = true;

        StateHasChanged();
    }

    private void Confirm(bool confirmed)
    {
        _isVisible = false;
        OnConfirm.InvokeAsync(confirmed);
    }
}
