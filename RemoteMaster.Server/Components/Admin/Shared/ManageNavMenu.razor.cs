// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace RemoteMaster.Server.Components.Admin.Shared;

public partial class ManageNavMenu
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private string _username = string.Empty;
    private string _role = string.Empty;

    protected async override Task OnInitializedAsync()
    {
        var authenticationState = await AuthenticationStateTask;
        var userPrincipal = authenticationState.User;

        if (userPrincipal.Identity != null && userPrincipal.Identity.IsAuthenticated)
        {
            var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                var user = await UserManager.FindByIdAsync(userId);

                if (user != null)
                {
                    var roles = await UserManager.GetRolesAsync(user);

                    _username = user.UserName ?? string.Empty;
                    _role = roles.FirstOrDefault() ?? string.Empty;
                }
            }
        }
    }
}
