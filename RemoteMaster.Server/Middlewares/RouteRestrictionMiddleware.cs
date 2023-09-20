// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Middlewares;

public class RouteRestrictionMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly List<string> RestrictedRoutes = new()
    {
        "/identity/account/manage/personaldata",
        "/identity/account/manage/deletepersonaldata"
    };

    private static readonly List<string> AllowedRoutes = new()
    {
        "/identity/account/login",
        "/identity/account/logout",
        "/identity/account/accessdenied",
        "/identity/account/manage"
    };

    public RouteRestrictionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var path = context.Request.Path.Value.ToLower();

        if (RestrictedRoutes.Contains(path))
        {
            context.Response.StatusCode = 404;

            return;
        }

        var isAllowedRoute = AllowedRoutes.Any(path.StartsWith);

        if (path.StartsWith("/identity/account") && !isAllowedRoute)
        {
            context.Response.Redirect("/Identity/Account/AccessDenied");

            return;
        }

        await _next(context);
    }
}
