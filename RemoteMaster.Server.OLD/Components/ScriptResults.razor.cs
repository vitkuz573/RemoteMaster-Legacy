// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

public partial class ScriptResults
{
    [Parameter]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Required for Blazor parameter")]
    public Dictionary<Computer, string> Results { get; set; }
}
