// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class Logout
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; }

    protected async override Task OnInitializedAsync()
    {
        await SignInManager.SignOutAsync();

        var userId = UserManager.GetUserId(HttpContext.User);

        if (!string.IsNullOrEmpty(userId))
        {
            await TokenService.RevokeAllRefreshTokensAsync(userId, TokenRevocationReason.UserLoggedOut);
            await TokenStorageService.ClearTokensAsync(userId);
        }

        NavigationManager.NavigateTo("/Account/Login", true);
    }
}
