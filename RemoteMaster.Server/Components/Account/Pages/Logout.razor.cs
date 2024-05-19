// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class Logout
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; }

    protected async override Task OnInitializedAsync()
    {
        await SignInManager.SignOutAsync();

        var refreshToken = HttpContext?.Request.Cookies[CookieNames.RefreshToken];

        if (refreshToken != null)
        {
            await TokenService.RevokeRefreshTokenAsync(refreshToken, TokenRevocationReason.UserLoggedOut);
        }

        HttpContext?.Response.Cookies.Delete(CookieNames.AccessToken);
        HttpContext?.Response.Cookies.Delete(CookieNames.RefreshToken);

        NavigationManager.NavigateTo("/Account/Login", true);
    }
}
