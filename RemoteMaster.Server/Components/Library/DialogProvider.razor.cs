// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Routing;
using RemoteMaster.Server.Components.Library.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogProvider : IDisposable
{
    private readonly List<IDialogReference> _dialogs = [];

    protected override void OnInitialized()
    {
        DialogService.OnDialogInstanceAdded += AddInstance;
        DialogService.OnDialogCloseRequested += DismissInstance;
        NavigationManager.LocationChanged += LocationChanged;
    }

    internal void DismissInstance(Guid id)
    {
        var reference = GetDialogReference(id);
        
        if (reference != null)
        {
            DismissInstance(reference);
        }
    }

    private void AddInstance(IDialogReference dialog)
    {
        Log.Information("Adding new dialog instance with ID: {DialogId}", dialog.Id);

        _dialogs.Add(dialog);

        InvokeAsync(StateHasChanged);
    }

    public void DismissAll()
    {
        _dialogs.ToList().ForEach(r => DismissInstance(r));
        
        StateHasChanged();
    }

    private void DismissInstance(IDialogReference dialog)
    {
        if (dialog != null)
        {
            Log.Information("Removing dialog instance with ID: {DialogId}", dialog.Id);
            
            _dialogs.Remove(dialog);
            
            InvokeAsync(StateHasChanged);
        }
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