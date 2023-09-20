// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Middlewares;

public class RegistrationRestrictionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _enableRegistration;
    private static readonly List<string> RestrictedRoutes = new()
    {
        "/identity/account/register"
    };

    public RegistrationRestrictionMiddleware(RequestDelegate next, bool enableRegistration)
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
        var isRestrictedRoute = RestrictedRoutes.Any(path.StartsWith);

        if (!_enableRegistration && isRestrictedRoute)
        {
            context.Response.Redirect("/Identity/Account/AccessDenied");

            return;
        }

        await _next(context);
    }
}
