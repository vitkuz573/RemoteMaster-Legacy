// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Shared;

public partial class LoginDisplay
{
    [Inject]
    public NavigationManager NavigationManager { get; set; }

    private void Logout()
    {
        NavigationManager.NavigateTo("Identity/Account", true);
    }

    private void Profile()
    {
        NavigationManager.NavigateTo("Identity/Account/Manage", true);
    }
}
