// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

public partial class ScriptResultPage
{
    [Parameter]
    [SuppressMessage("CRITICAL", "CA2227")]
    public Dictionary<Computer, string> Results { get; set; }
}
