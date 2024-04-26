// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Extensions;

public static class HttpContextExtensions
{
    public static void SetCookie(this HttpContext context, string key, string value, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.Add(duration)
        };

        context.Response.Cookies.Append(key, value, options);
    }
}
