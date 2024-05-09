// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Components.Library;

public partial class Dialog
{
    private string Title { get; set; } = string.Empty;

    private string Message { get; set; } = string.Empty;

    private string ConfirmText { get; set; } = "OK";

    private string CancelText { get; set; } = "Cancel";

    private bool Show { get; set; } = false;

    private bool IsConfirmation { get; set; } = false;

    private TaskCompletionSource<bool> ConfirmationResult { get; set; }

    protected override void OnInitialized()
    {
        DialogService.OnShowDialog += ShowDialog;
        DialogService.OnShowConfirmationDialog += ShowConfirmationDialog;
    }

    private void ShowDialog(string title, string message)
    {
        Title = title;
        Message = message;
        Show = true;
        IsConfirmation = false;

        StateHasChanged();
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

        StateHasChanged();

        return await ConfirmationResult.Task;
    }

    private void CloseDialog()
    {
        Show = false;

        StateHasChanged();
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
}
