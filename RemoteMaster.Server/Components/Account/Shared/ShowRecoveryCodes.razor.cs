// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Account.Shared;

public partial class ShowRecoveryCodes
{
    [Parameter]
    public string[] RecoveryCodes { get; set; } = [];

    [Parameter]
    public string? StatusMessage { get; set; }
}
