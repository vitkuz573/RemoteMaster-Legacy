// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Components.Library.Services;

public class DialogService : IDialogWindowService
{
    public event Action<IDialogReference> OnDialogInstanceAdded;

    public Task<IDialogReference> ShowAsync<T>(string title) where T : IComponent
    {
        var dialogReference = CreateReference();

        var dialogFragment = new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(T));
            builder.AddAttribute(1, "Title", title);
            builder.CloseComponent();
        });

        dialogReference.InjectRenderFragment(dialogFragment);

        Log.Information("Creating dialog of type {DialogType} with title '{Title}' and ID {DialogId}", typeof(T).Name, title, dialogReference.Id);
        
        OnDialogInstanceAdded?.Invoke(dialogReference);

        return Task.FromResult(dialogReference);
    }

    public IDialogReference CreateReference()
    {
        return new DialogReference(Guid.NewGuid(), new DialogInstance());
    }
}