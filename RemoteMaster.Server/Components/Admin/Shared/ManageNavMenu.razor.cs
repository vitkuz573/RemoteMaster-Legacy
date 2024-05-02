// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.


using System.Security.Claims;

namespace RemoteMaster.Server.Components.Admin.Shared;

public partial class ManageNavMenu
{
    private string _username;
    private string _role;

    protected async override Task OnInitializedAsync()
    {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userPrincipal = authenticationState.User;

        if (userPrincipal.Identity.IsAuthenticated)
        {
            var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                var user = await UserManager.FindByIdAsync(userId);

                var roles = await UserManager.GetRolesAsync(user);

                _role = roles.FirstOrDefault();
            }
        }

        _username = userPrincipal.Identity.Name;
    }
}
