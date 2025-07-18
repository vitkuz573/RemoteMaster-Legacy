﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

namespace RemoteMaster.Server.Components.Account.Pages.Manage;

public partial class Disable2fa
{
    private ApplicationUser _user = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    protected async override Task OnInitializedAsync()
    {
        _user = await UserAccessor.GetRequiredUserAsync(HttpContext);

        if (HttpMethods.IsGet(HttpContext.Request.Method) && !await UserManager.GetTwoFactorEnabledAsync(_user))
        {
            throw new InvalidOperationException("Cannot disable 2FA for user as it's not currently enabled.");
        }
    }

    private async Task OnSubmitAsync()
    {
        var disable2faResult = await UserManager.SetTwoFactorEnabledAsync(_user, false);

        if (!disable2faResult.Succeeded)
        {
            throw new InvalidOperationException("Unexpected error occurred disabling 2FA.");
        }

        var userId = await UserManager.GetUserIdAsync(_user);

        Logger.LogInformation("User with ID '{UserId}' has disabled 2fa.", userId);

        RedirectManager.RedirectToWithStatus("Account/Manage/TwoFactorAuthentication", "2fa has been disabled. You can reenable 2fa when you setup an authenticator app", HttpContext);
    }
}