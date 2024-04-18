// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class Logout
{
    protected async override Task OnInitializedAsync()
    {
        await SignInManager.SignOutAsync();

        NavigationManager.NavigateTo("/Account/Login", true);
    }
}
