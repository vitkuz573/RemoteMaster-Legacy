// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

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
            => new(builder =>
            {
                builder.OpenComponent<DialogHelperComponent>(1);
                builder.AddAttribute(2, ChildContent, renderFragment);
                builder.CloseComponent();
            });
    }

    public event Action<IDialogReference> OnDialogInstanceAdded;
    public event Action<IDialogReference> OnDialogCloseRequested;

    public IDialogReference Show(Type contentComponent, string title)
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
            builder.AddComponentReferenceCapture(i++, inst => { dialogReference.InjectDialog(inst); });
            builder.CloseComponent();
        }));

        var dialogInstance = new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(DialogInstance));
            builder.AddAttribute(1, "Title", title);
            builder.AddAttribute(2, "Id", dialogReference.Id);
            builder.CloseComponent();
        });

        dialogReference.InjectRenderFragment(dialogInstance);

        OnDialogInstanceAdded?.Invoke(dialogReference);

        return dialogReference;
    }

    public Task<IDialogReference> ShowAsync<T>(string title) where T : IComponent
    {
        return ShowAsync(typeof(T), title);
    }

    public async Task<IDialogReference> ShowAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type contentComponent, string title)
    {
        var dialogReference = Show(contentComponent, title);

        return dialogReference;
    }

    public virtual void Close(IDialogReference dialog)
    {
        OnDialogCloseRequested?.Invoke(dialog);
    }

    public virtual IDialogReference CreateReference()
    {
        return new DialogReference(Guid.NewGuid(), this);
    }
}