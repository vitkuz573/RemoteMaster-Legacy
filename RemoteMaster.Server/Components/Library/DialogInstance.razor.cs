// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogInstance : ComponentBase, IDisposable
{
    private readonly string _elementId = $"dialog_{Guid.NewGuid().ToString()[..8]}";

    [CascadingParameter]
    private DialogProvider Parent { get; set; }

    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public RenderFragment TitleContent { get; set; }

    [Parameter]
    public RenderFragment Content { get; set; }

    [Parameter]
    public Guid Id { get; set; }

    private bool NoHeader { get; set; }

    protected override void OnInitialized()
    {
        ConfigureInstance();
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
    }

#pragma warning disable
    private bool SetHideHeader()
    {
        return false;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}