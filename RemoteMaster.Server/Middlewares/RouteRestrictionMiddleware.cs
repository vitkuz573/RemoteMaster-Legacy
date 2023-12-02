// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Middlewares;

public class RouteRestrictionMiddleware(RequestDelegate next)
{
    private static readonly List<string> RestrictedRoutes =
    [
        "/account/manage/personaldata",
        "/account/manage/deletepersonaldata"
    ];

    private static readonly List<string> AllowedRoutes =
    [
        "/account/login",
        "/account/logout",
        "/account/manage",
        "/account/register"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var path = context.Request.Path.Value.ToLower();

        if (RestrictedRoutes.Contains(path))
        {
            context.Response.StatusCode = 404;

            return;
        }

        var isAllowedRoute = AllowedRoutes.Any(path.StartsWith);

        if (path.StartsWith("/account") && !isAllowedRoute)
        {
            context.Response.Redirect("/");

            return;
        }

        await next(context);
    }
}
