// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Enums;

namespace RemoteMaster.Server.Components.Library;

public partial class TextBlock
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public string TextClass { get; set; } = string.Empty;

    [Parameter]
    public TypoType Typo { get; set; } = TypoType.Span;

#pragma warning disable CA2227
    [Parameter]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = [];
#pragma warning restore CA2227
}
