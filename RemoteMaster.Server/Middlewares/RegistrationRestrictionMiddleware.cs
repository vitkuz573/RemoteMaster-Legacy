// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Middlewares;

public class RegistrationRestrictionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = context.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();

        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var isRegistrationRoute = path.StartsWith("/account/register");

        if (isRegistrationRoute && await RootAdministratorExists(userManager, roleManager))
        {
            context.Response.Redirect("/");
            
            return;
        }

        await next(context);
    }

    private static async Task<bool> RootAdministratorExists(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        var roleExists = await roleManager.RoleExistsAsync("RootAdministrator");
        
        if (!roleExists)
        {
            return false;
        }

        var users = await userManager.GetUsersInRoleAsync("RootAdministrator");
        
        return users.Any();
    }
}


