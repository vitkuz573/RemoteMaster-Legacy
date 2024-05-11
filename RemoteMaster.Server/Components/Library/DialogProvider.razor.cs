// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using RemoteMaster.Server.Components.Library.Abstractions;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogProvider : IDisposable
{
    private readonly List<IDialogReference> _dialogs = [];
    private readonly DialogOptions _globalDialogOptions = new();

    [Parameter]
    public bool? NoHeader { get; set; }

    [Parameter]
    public bool? CloseButton { get; set; }

    [Parameter]
    public bool? BackdropClick { get; set; }

    [Parameter]
    public bool? CloseOnEscapeKey { get; set; }

    [Parameter]
    public bool? FullWidth { get; set; }

    [Parameter]
    public string? BackgroundClass { get; set; }

    protected override void OnInitialized()
    {
        DialogService.OnDialogInstanceAdded += AddInstance;
        DialogService.OnDialogCloseRequested += DismissInstance;
        NavigationManager.LocationChanged += LocationChanged;

        _globalDialogOptions.BackdropClick = BackdropClick;
        _globalDialogOptions.CloseOnEscapeKey = CloseOnEscapeKey;
        _globalDialogOptions.CloseButton = CloseButton;
        _globalDialogOptions.NoHeader = NoHeader;
        _globalDialogOptions.FullWidth = FullWidth;
        _globalDialogOptions.BackgroundClass = BackgroundClass;
    }

    internal void DismissInstance(Guid id, DialogResult result)
    {
        var reference = GetDialogReference(id);

        if (reference != null)
        {
            DismissInstance(reference, result);
        }
    }

    private void AddInstance(IDialogReference dialog)
    {
        _dialogs.Add(dialog);

        InvokeAsync(StateHasChanged);
    }

    public void DismissAll()
    {
        _dialogs.ToList().ForEach(r => DismissInstance(r, DialogResult.Cancel()));
        
        StateHasChanged();
    }

    private void DismissInstance(IDialogReference dialog, DialogResult result)
    {
        if (!dialog.Dismiss(result))
        {
            return;
        }

        _dialogs.Remove(dialog);

        StateHasChanged();
    }

    private IDialogReference? GetDialogReference(Guid id)
    {
        return _dialogs.SingleOrDefault(x => x.Id == id);
    }

    private void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        DismissAll();
    }

    public void Dispose()
    {
        if (NavigationManager != null)
        {
            NavigationManager.LocationChanged -= LocationChanged;
        }

        if (DialogService != null)
        {
            DialogService.OnDialogInstanceAdded -= AddInstance;
            DialogService.OnDialogCloseRequested -= DismissInstance;
        }
    }
}