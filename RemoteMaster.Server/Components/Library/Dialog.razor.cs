// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RemoteMaster.Server.Components.Library;

public partial class Dialog : IDisposable
{
    private string Title { get; set; } = string.Empty;

    private string Message { get; set; } = string.Empty;

    private string ConfirmText { get; set; } = "OK";

    private string CancelText { get; set; } = "Cancel";

    private bool Show { get; set; } = false;

    private bool IsConfirmation { get; set; } = false;

    private TaskCompletionSource<bool> ConfirmationResult { get; set; }

    [Parameter]
    public bool CloseOnClickOutside { get; set; } = false;

    [Parameter]
    public bool CloseOnEsc { get; set; } = false;

    private DotNetObjectReference<Dialog>? _dialogRef;

    protected override void OnInitialized()
    {
        DialogService.OnShowDialog += ShowDialog;
        DialogService.OnShowConfirmationDialog += ShowConfirmationDialog;
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dialogRef = DotNetObjectReference.Create(this);

            if (CloseOnEsc)
            {
                await JsRuntime.InvokeVoidAsync("addGlobalKeydownListener", _dialogRef);
            }
        }
    }

    [JSInvokable]
    public void HandleKeyDown(string key)
    {
        if (key == "Escape" && Show)
        {
            CloseDialog();
        }
    }

    private void CloseOnOutsideClick()
    {
        if (CloseOnClickOutside)
        {
            CloseDialog();
        }
    }

    private void ShowDialog(string title, string message)
    {
        Title = title;
        Message = message;
        Show = true;
        IsConfirmation = false;

        InvokeAsync(StateHasChanged);
    }

    private async Task<bool> ShowConfirmationDialog(string title, string message, string confirmText, string cancelText)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        Show = true;
        IsConfirmation = true;
        ConfirmationResult = new TaskCompletionSource<bool>();

        await InvokeAsync(StateHasChanged);

        return await ConfirmationResult.Task;
    }

    private void CloseDialog()
    {
        Show = false;

        InvokeAsync(StateHasChanged);
    }

    private void Confirm()
    {
        ConfirmationResult?.SetResult(true);
        CloseDialog();
    }

    private void Cancel()
    {
        ConfirmationResult?.SetResult(false);
        CloseDialog();
    }

    public void Dispose()
    {
        _dialogRef?.Dispose();
    }
}
