// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Extensions;

public static class RequestCookieCollectionExtensions
{
    public static string GetCookieOrDefault(this IRequestCookieCollection cookies, string key, string defaultValue = "")
    {
        return cookies == null
            ? throw new ArgumentNullException(nameof(cookies))
            : cookies.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public static bool HasKeyIgnoringCase(this IRequestCookieCollection cookies, string key)
    {
        return cookies == null
            ? throw new ArgumentNullException(nameof(cookies))
            : cookies.Keys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}