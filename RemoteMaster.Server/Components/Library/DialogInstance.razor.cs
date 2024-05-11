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
        Parent.DismissInstance(Id);
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