// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Middlewares;

public class PortRestrictionMiddleware(RequestDelegate next, int restrictedPort)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Connection.LocalPort == restrictedPort && !context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 404;

            return;
        }

        await next(context);
    }
}
