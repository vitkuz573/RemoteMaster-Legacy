// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RemoteMaster.Server.Components.Library.Abstractions;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library.Services;

public class DialogService : IDialogWindowService
{
    private class DialogHelperComponent : ComponentBase
    {
        private static readonly Dictionary<RenderFragment, RenderFragment> _fragmentCache = new Dictionary<RenderFragment, RenderFragment>();
        private RenderFragment _renderFragment;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected override void OnParametersSet()
        {
            if (ChildContent != null)
            {
                _renderFragment = ChildContent;

                StateHasChanged();
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            base.BuildRenderTree(builder);

            if (_renderFragment != null)
            {
                builder.AddContent(0, _renderFragment);
            }
        }

        public static RenderFragment Wrap(RenderFragment renderFragment)
        {
            if (!_fragmentCache.TryGetValue(renderFragment, out var cachedFragment))
            {
                cachedFragment = builder =>
                {
                    builder.OpenComponent<DialogHelperComponent>(1);
                    builder.AddAttribute(2, "ChildContent", renderFragment);
                    builder.CloseComponent();
                };

                _fragmentCache[renderFragment] = cachedFragment;
            }

            return cachedFragment;
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            if (ChildContent == null)
            {
                throw new ArgumentNullException(nameof(ChildContent), "ChildContent cannot be null.");
            }

            return base.SetParametersAsync(ParameterView.Empty);
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

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var token = cancellationTokenSource.Token;

        await using (token.Register(() => dialogReference.RenderCompleteTaskCompletionSource.TrySetResult(false)))
        {
            await dialogReference.RenderCompleteTaskCompletionSource.Task;

            return dialogReference;
        }
    }

    public Task<bool?> ShowMessageBox(string title, string message, string yesText = "OK", string? noText = null, string? cancelText = null, DialogOptions? options = null)
    {
        return ShowMessageBox(new MessageBoxOptions
        {
            Title = title,
            Message = message,
            YesText = yesText,
            NoText = noText,
            CancelText = cancelText,
        }, options);
    }

    public Task<bool?> ShowMessageBox(string title, MarkupString markupMessage, string yesText = "OK", string? noText = null, string? cancelText = null, DialogOptions? options = null)
    {
        return ShowMessageBox(new MessageBoxOptions
        {
            Title = title,
            MarkupMessage = markupMessage,
            YesText = yesText,
            NoText = noText,
            CancelText = cancelText,
        }, options);
    }

    public async Task<bool?> ShowMessageBox(MessageBoxOptions messageBoxOptions, DialogOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(messageBoxOptions);

        var parameters = new DialogParameters()
        {
            [nameof(MessageBoxOptions.Title)] = messageBoxOptions.Title,
            [nameof(MessageBoxOptions.Message)] = messageBoxOptions.Message,
            [nameof(MessageBoxOptions.MarkupMessage)] = messageBoxOptions.MarkupMessage,
            [nameof(MessageBoxOptions.CancelText)] = messageBoxOptions.CancelText,
            [nameof(MessageBoxOptions.NoText)] = messageBoxOptions.NoText,
            [nameof(MessageBoxOptions.YesText)] = messageBoxOptions.YesText,
        };
        
        var reference = await ShowAsync<MessageBox>(title: messageBoxOptions.Title, parameters: parameters, options: options);
        var result = await reference.Result;
        
        if (result.Canceled || result.Data is not bool data)
        {
            return null;
        }

        return data;
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