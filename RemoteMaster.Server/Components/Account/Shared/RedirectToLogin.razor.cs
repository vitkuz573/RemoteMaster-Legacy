// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Components.Account.Shared;

public partial class RedirectToLogin
{
    protected override void OnInitialized()
    {
        NavigationManager.NavigateTo($"Account/Login?returnUrl={Uri.EscapeDataString(NavigationManager.Uri)}", forceLoad: true);
    }
}
