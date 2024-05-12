// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RemoteMaster.Server.Components.Library.Abstractions;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library;

public partial class MessageBox : ComponentBase
{
    private bool _visible;
    private IDialogReference? _reference;
    private ActivatableCallback? _yesCallback, _cancelCallback, _noCallback;

    [CascadingParameter]
    private DialogInstance? DialogInstance { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    [Parameter]
    public string? Message { get; set; }

    [Parameter]
    public MarkupString MarkupMessage { get; set; }

    [Parameter]
    public RenderFragment? MessageContent { get; set; }

    [Parameter]
    public string? CancelText { get; set; }

    [Parameter]
    public RenderFragment? CancelButton { get; set; }

    [Parameter]
    public string? NoText { get; set; }

    [Parameter]
    public RenderFragment? NoButton { get; set; }

    [Parameter]
    public string YesText { get; set; } = "OK";

    [Parameter]
    public RenderFragment? YesButton { get; set; }

    [Parameter]
    public EventCallback<bool> OnYes { get; set; }

    [Parameter]
    public EventCallback<bool> OnNo { get; set; }

    [Parameter]
    public EventCallback<bool> OnCancel { get; set; }

    [Parameter]
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value)
            {
                return;
            }

            _visible = value;

            if (IsInline)
            {
                if (_visible)
                {
                    _ = Show();
                }
                else
                {
                    Close();
                }
            }

            VisibleChanged.InvokeAsync(value);
        }
    }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    private bool IsInline => DialogInstance == null;

    public async Task<bool?> Show(DialogOptions? options = null)
    {
        var parameters = new DialogParameters
        {
            [nameof(Title)] = Title,
            [nameof(TitleContent)] = TitleContent,
            [nameof(Message)] = Message,
            [nameof(MarkupMessage)] = MarkupMessage,
            [nameof(MessageContent)] = MessageContent,
            [nameof(CancelText)] = CancelText,
            [nameof(CancelButton)] = CancelButton,
            [nameof(NoText)] = NoText,
            [nameof(NoButton)] = NoButton,
            [nameof(YesText)] = YesText,
            [nameof(YesButton)] = YesButton,
        };

        _reference = await DialogService.ShowAsync<MessageBox>(title: Title, parameters: parameters, options: options);
        
        var result = await _reference.Result;
        
        if (result.Canceled || result.Data is not bool data)
        {
            return null;
        }

        return data;
    }

    public void Close()
    {
        _reference?.Close();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        if (YesButton is not null)
        {
            _yesCallback = new ActivatableCallback
            {
                ActivateCallback = OnYesActivated
            };
        }

        if (NoButton is not null)
        {
            _noCallback = new ActivatableCallback
            {
                ActivateCallback = OnNoActivated
            };
        }

        if (CancelButton is not null)
        {
            _cancelCallback = new ActivatableCallback
            {
                ActivateCallback = OnCancelActivated
            };
        }
    }

    private void OnYesActivated(object arg1, MouseEventArgs arg2) => OnYesClicked();

    private void OnNoActivated(object arg1, MouseEventArgs arg2) => OnNoClicked();

    private void OnCancelActivated(object arg1, MouseEventArgs arg2) => OnCancelClicked();

    private void OnYesClicked() => DialogInstance?.Close(DialogResult.Ok(true));

    private void OnNoClicked() => DialogInstance?.Close(DialogResult.Ok(false));

    private void OnCancelClicked() => DialogInstance?.Close(DialogResult.Cancel());

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            OnCancelClicked();
        }
    }
}
