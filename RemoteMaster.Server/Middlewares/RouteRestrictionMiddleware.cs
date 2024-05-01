// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Middlewares;

public class RouteRestrictionMiddleware(RequestDelegate next)
{
    private static readonly List<string> RootAdminExclusiveRoutes =
    [
        "/admin"
    ];

    private static readonly List<string> RestrictedRoutes =
    [
        "/account/manage/personaldata",
        "/account/manage/deletepersonaldata"
    ];

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userManager);

        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var user = await userManager.GetUserAsync(context.User);

        if (user != null)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            
            var isRootAdmin = userRoles.Contains("RootAdministrator");
            var isAdmin = userRoles.Contains("Administrator");

            if (isRootAdmin && RootAdminExclusiveRoutes.Any(path.StartsWith))
            {
                await next(context);

                return;
            }

            if (!isRootAdmin && (RestrictedRoutes.Contains(path) || path.StartsWith("/admin")))
            {
                context.Response.StatusCode = 403;

                return;
            }

            if (path.StartsWith("/account") && (!isRootAdmin && !isAdmin))
            {
                context.Response.Redirect("/");

                return;
            }
        }

        await next(context);
    }
}
