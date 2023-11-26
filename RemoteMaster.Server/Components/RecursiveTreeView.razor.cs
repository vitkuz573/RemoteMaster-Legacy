// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

public partial class RecursiveTreeView
{
    [Parameter]
    public Node Node { get; set; }
}
