// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Middlewares;

public class RegistrationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _enableRegistration;

    private static readonly List<string> RestrictedRoutes = new()
    {
        "/identity/account/register",
        "/identity/account/manage/personaldata"
    };

    private static readonly List<string> AllowedRoutes = new()
    {
        "/identity/account/login",
        "/identity/account/logout",
        "/identity/account/accessdenied",
        "/identity/account/manage"
    };

    private static readonly List<string> AllowedManageRoutes = new()
    {
        "/identity/account/manage"
    };

    public RegistrationMiddleware(RequestDelegate next, bool enableRegistration)
    {
        _next = next;
        _enableRegistration = enableRegistration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var path = context.Request.Path.Value.ToLower();

        // Проверка для маршрута "/identity/account/manage/personaldata"
        if (path == "/identity/account/manage/personaldata")
        {
            context.Response.StatusCode = 404;

            return;
        }

        var isRestrictedRoute = RestrictedRoutes.Any(path.StartsWith);
        var isAllowedRoute = AllowedRoutes.Any(path.StartsWith);
        var isAllowedManageRoute = AllowedManageRoutes.Any(path.StartsWith);

        if (!_enableRegistration && isRestrictedRoute ||
            path.StartsWith("/identity/account") &&
            !isAllowedRoute &&
            !isRestrictedRoute &&
            !isAllowedManageRoute)
        {
            context.Response.Redirect("/Identity/Account/AccessDenied");

            return;
        }

        await _next(context);
    }
}
