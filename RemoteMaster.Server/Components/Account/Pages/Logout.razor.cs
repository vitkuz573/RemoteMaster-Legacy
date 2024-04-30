// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Account.Pages;

public partial class Logout
{
    protected async override Task OnInitializedAsync()
    {
        await SignInManager.SignOutAsync();

        var httpContext = HttpContextAccessor.HttpContext;

        var refreshToken = httpContext?.Request.Cookies[CookieNames.RefreshToken];

        if (refreshToken != null)
        {
            await TokenService.RevokeRefreshTokenAsync(refreshToken, TokenRevocationReason.UserLoggedOut);
        }

        httpContext?.Response.Cookies.Delete(CookieNames.AccessToken);
        httpContext?.Response.Cookies.Delete(CookieNames.RefreshToken);

        NavigationManager.NavigateTo("/Account/Login", true);
    }
}
