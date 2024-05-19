// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Data;
using Serilog;

namespace RemoteMaster.Server.Components.Account.Pages.Manage;

public partial class GenerateRecoveryCodes
{
    private string? message;
    private ApplicationUser user = default!;
    private IEnumerable<string>? recoveryCodes;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    protected async override Task OnInitializedAsync()
    {
        user = await UserAccessor.GetRequiredUserAsync(HttpContext);

        var isTwoFactorEnabled = await UserManager.GetTwoFactorEnabledAsync(user);
        
        if (!isTwoFactorEnabled)
        {
            throw new InvalidOperationException("Cannot generate recovery codes for user because they do not have 2FA enabled.");
        }
    }

    private async Task OnSubmitAsync()
    {
        var userId = await UserManager.GetUserIdAsync(user);
        
        recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        message = "You have generated new recovery codes.";

        Log.Information("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
    }
}
