// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library;

public partial class TextBlock
{
    [Parameter]
    public string Content { get; set; } = string.Empty;

    [Parameter]
    public string TextClass { get; set; } = string.Empty;

    [Parameter]
    public string Typo { get; set; } = "span";

#pragma warning disable CA2227
    [Parameter]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = [];
#pragma warning restore CA2227
}
