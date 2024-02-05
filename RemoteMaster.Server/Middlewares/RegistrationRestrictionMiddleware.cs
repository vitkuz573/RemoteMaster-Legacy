// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Middlewares;

public class RegistrationRestrictionMiddleware(RequestDelegate next, bool enableRegistration)
{
    private static readonly List<string> RestrictedRoutes =
    [
        "/account/register"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var isRestrictedRoute = RestrictedRoutes.Any(path.StartsWith);

        if (!enableRegistration && isRestrictedRoute)
        {
            context.Response.Redirect("/");

            return;
        }

        await next(context);
    }
}
