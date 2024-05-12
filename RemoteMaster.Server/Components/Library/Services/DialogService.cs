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

    public IDialogReference Show<T>() where T : IComponent
    {
        return Show<T>(string.Empty, [], new DialogOptions());
    }

    public IDialogReference Show<T>(string title) where T : IComponent
    {
        return Show<T>(title, [], new DialogOptions());
    }

    public IDialogReference Show<T>(string title, DialogOptions options) where T : IComponent
    {
        return Show<T>(title, [], options);
    }

    public IDialogReference Show<T>(string title, DialogParameters parameters) where T : IComponent
    {
        return Show<T>(title, parameters, new DialogOptions());
    }

    public IDialogReference Show<T>(string title, DialogParameters parameters, DialogOptions options) where T : IComponent
    {
        return Show(typeof(T), title, parameters, options);
    }

    public IDialogReference Show(Type contentComponent)
    {
        return Show(contentComponent, string.Empty, [], new DialogOptions());
    }

    public IDialogReference Show(Type contentComponent, string title)
    {
        return Show(contentComponent, title, [], new DialogOptions());
    }

    public IDialogReference Show(Type contentComponent, string title, DialogOptions options)
    {
        return Show(contentComponent, title, [], options);
    }

    public IDialogReference Show(Type contentComponent, string title, DialogParameters parameters)
    {
        return Show(contentComponent, title, parameters, new DialogOptions());
    }

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

            builder.AddComponentReferenceCapture(i++, instance =>
            {
                dialogReference.InjectDialog(instance);
            });

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

    public Task<IDialogReference> ShowAsync<T>() where T : IComponent
    {
        return ShowAsync<T>(string.Empty, [], new DialogOptions());
    }

    public Task<IDialogReference> ShowAsync<T>(string title) where T : IComponent
    {
        return ShowAsync<T>(title, [], new DialogOptions());
    }

    public Task<IDialogReference> ShowAsync<T>(string title, DialogOptions options) where T : IComponent
    {
        return ShowAsync<T>(title, [], options);
    }

    public Task<IDialogReference> ShowAsync<T>(string title, DialogParameters parameters) where T : IComponent
    {
        return ShowAsync<T>(title, parameters, new DialogOptions());
    }

    public Task<IDialogReference> ShowAsync<T>(string title, DialogParameters parameters, DialogOptions options) where T : IComponent
    {
        return ShowAsync(typeof(T), title, parameters, options);
    }

    public Task<IDialogReference> ShowAsync(Type contentComponent)
    {
        return ShowAsync(contentComponent, string.Empty, [], new DialogOptions());
    }

    public Task<IDialogReference> ShowAsync(Type contentComponent, string title)
    {
        return ShowAsync(contentComponent, title, [], new DialogOptions());
    }

    public Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogOptions options)
    {
        return ShowAsync(contentComponent, title, [], options);
    }

    public Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogParameters parameters)
    {
        return ShowAsync(contentComponent, title, parameters, new DialogOptions());
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