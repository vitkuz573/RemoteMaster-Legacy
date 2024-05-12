// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library.Services;

public class DialogService : IDialogWindowService
{
    private class DialogHelperComponent : IComponent
    {
        private const string ChildContent = nameof(ChildContent);
        private RenderFragment _renderFragment;
        private RenderHandle _renderHandle;
        
        void IComponent.Attach(RenderHandle renderHandle) => _renderHandle = renderHandle;
        
        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            if (_renderFragment == null)
            {
                if (parameters.TryGetValue(ChildContent, out _renderFragment))
                {
                    _renderHandle.Render(_renderFragment);
                }
            }

            return Task.CompletedTask;
        }

        public static RenderFragment Wrap(RenderFragment renderFragment)
        {
            return new RenderFragment(builder =>
            {
                builder.OpenComponent<DialogHelperComponent>(1);
                builder.AddAttribute(2, ChildContent, renderFragment);
                builder.CloseComponent();
            });
        }
    }

    public event Action<IDialogReference> OnDialogInstanceAdded;
    public event Action<IDialogReference, DialogResult> OnDialogCloseRequested;

    public IDialogReference Show(Type contentComponent, string title, DialogParameters parameters, DialogOptions options)
    {
        if (!typeof(IComponent).IsAssignableFrom(contentComponent))
        {
            throw new ArgumentException($"{contentComponent?.FullName} must be a Blazor IComponent");
        }

        var dialogReference = CreateReference();

        var dialogContent = DialogHelperComponent.Wrap(new RenderFragment(builder =>
        {
            var i = 0;

            builder.OpenComponent(i++, contentComponent);

            foreach (var parameter in parameters)
            {
                builder.AddAttribute(i++, parameter.Key, parameter.Value);
            }

            builder.AddComponentReferenceCapture(i++, inst => { dialogReference.InjectDialog(inst); });
            builder.CloseComponent();
        }));

        var dialogInstance = new RenderFragment(builder =>
        {
            builder.OpenComponent<DialogInstance>(0);
            builder.SetKey(dialogReference.Id);
            builder.AddAttribute(1, nameof(DialogInstance.Options), options);
            builder.AddAttribute(2, nameof(DialogInstance.Title), title);
            builder.AddAttribute(3, nameof(DialogInstance.Content), dialogContent);
            builder.AddAttribute(4, nameof(DialogInstance.Id), dialogReference.Id);
            builder.CloseComponent();
        });

        dialogReference.InjectRenderFragment(dialogInstance);

        OnDialogInstanceAdded?.Invoke(dialogReference);

        return dialogReference;
    }

    public Task<IDialogReference> ShowAsync<T>(string title, DialogParameters parameters, DialogOptions options) where T : IComponent
    {
        return ShowAsync(typeof(T), title, parameters, options);
    }

    public async Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogParameters parameters, DialogOptions options)
    {
        var dialogReference = Show(contentComponent, title, parameters, options);

        return dialogReference;
    }

    public void Close(IDialogReference dialog)
    {
        Close(dialog, DialogResult.Ok<object>(null));
    }

    public virtual void Close(IDialogReference dialog, DialogResult result)
    {
        OnDialogCloseRequested?.Invoke(dialog, result);
    }

    public virtual IDialogReference CreateReference()
    {
        return new DialogReference(Guid.NewGuid(), this);
    }
}