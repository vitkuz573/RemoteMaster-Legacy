// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogInstance : ComponentBase, IDisposable
{
    private DialogOptions _options = new();
    private readonly string _elementId = $"dialog_{Guid.NewGuid().ToString()[..8]}";
    private Dialog _dialog;

    [CascadingParameter]
    private DialogProvider Parent { get; set; }

    [CascadingParameter]
    private DialogOptions GlobalDialogOptions { get; set; } = new DialogOptions();

    [Parameter]
    public DialogOptions Options
    {
        get
        {
            _options ??= new DialogOptions();

            return _options;
        }
        set => _options = value;
    }

    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public RenderFragment TitleContent { get; set; }

    [Parameter]
    public RenderFragment Content { get; set; }

    [Parameter]
    public Guid Id { get; set; }

    private string Position { get; set; }

    private string DialogMaxWidth { get; set; }

    private bool BackdropClick { get; set; } = true;

    private bool CloseOnEscapeKey { get; set; }

    private bool NoHeader { get; set; }

    private bool CloseButton { get; set; }

    private bool FullScreen { get; set; }

    private bool FullWidth { get; set; }

    protected override void OnInitialized()
    {
        ConfigureInstance();
    }

    public void SetOptions(DialogOptions options)
    {
        Options = options;
        ConfigureInstance();

        StateHasChanged();
    }

    public void SetTitle(string title)
    {
        Title = title;

        StateHasChanged();
    }

    public void Close()
    {
        Close(DialogResult.Ok<object>(null));
    }

    public void Close(DialogResult dialogResult)
    {
        Parent.DismissInstance(Id, dialogResult);
    }

    public void Close<T>(T returnValue)
    {
        var dialogResult = DialogResult.Ok<T>(returnValue);

        Parent.DismissInstance(Id, dialogResult);
    }

    public void Cancel()
    {
        Close(DialogResult.Cancel());
    }

    private void ConfigureInstance()
    {
        NoHeader = SetHideHeader();
        CloseButton = SetCloseButton();
        FullWidth = SetFullWidth();
        FullScreen = SetFullScreen();
        BackdropClick = SetBackdropClick();
        CloseOnEscapeKey = SetCloseOnEscapeKey();
    }

    private bool SetFullWidth()
    {
        if (Options.FullWidth.HasValue)
        {
            return Options.FullWidth.Value;
        }

        if (GlobalDialogOptions.FullWidth.HasValue)
        {
            return GlobalDialogOptions.FullWidth.Value;
        }

        return false;
    }

    private bool SetFullScreen()
    {
        if (Options.FullScreen.HasValue)
        {
            return Options.FullScreen.Value;
        }

        if (GlobalDialogOptions.FullScreen.HasValue)
        {
            return GlobalDialogOptions.FullScreen.Value;
        }

        return false;
    }

    private bool SetHideHeader()
    {
        if (Options.NoHeader.HasValue)
        {
            return Options.NoHeader.Value;
        }

        if (GlobalDialogOptions.NoHeader.HasValue)
        {
            return GlobalDialogOptions.NoHeader.Value;
        }

        return false;
    }

    private bool SetCloseButton()
    {
        if (Options.CloseButton.HasValue)
        {
            return Options.CloseButton.Value;
        }

        if (GlobalDialogOptions.CloseButton.HasValue)
        {
            return GlobalDialogOptions.CloseButton.Value;
        }

        return false;
    }

    private bool SetBackdropClick()
    {
        if (Options.BackdropClick.HasValue)
        {
            return Options.BackdropClick.Value;
        }

        if (GlobalDialogOptions.BackdropClick.HasValue)
        {
            return GlobalDialogOptions.BackdropClick.Value;
        }

        return true;
    }

    private bool SetCloseOnEscapeKey()
    {
        if (Options.CloseOnEscapeKey.HasValue)
        {
            return Options.CloseOnEscapeKey.Value;
        }

        if (GlobalDialogOptions.CloseOnEscapeKey.HasValue)
        {
            return GlobalDialogOptions.CloseOnEscapeKey.Value;
        }

        return false;
    }

    private async Task HandleBackgroundClickAsync(MouseEventArgs args)
    {
        if (!BackdropClick)
        {
            return;
        }

        if (_dialog is null || !_dialog.OnBackdropClick.HasDelegate)
        {
            Cancel();
            return;
        }

        await _dialog.OnBackdropClick.InvokeAsync(args);
    }

    public void Register(Dialog dialog)
    {
        if (dialog == null)
        {
            return;
        }

        _dialog = dialog;
        TitleContent = dialog.TitleContent;

        StateHasChanged();
    }

    public void CancelAll()
    {
        Parent?.DismissAll();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}