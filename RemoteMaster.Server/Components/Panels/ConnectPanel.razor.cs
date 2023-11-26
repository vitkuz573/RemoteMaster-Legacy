// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Panels;

public partial class ConnectPanel
{
    [Parameter]
    public ConnectData Content { get; set; }
}
