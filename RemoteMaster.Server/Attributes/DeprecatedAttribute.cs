// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc.Filters;

namespace RemoteMaster.Server.Attributes;

public class DeprecatedAttribute(string message = "This API version is deprecated and will be removed in a future release.", string link = "") : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var responseHeaders = context.HttpContext.Response.Headers;

        responseHeaders.Add("Deprecation", "true");

        if (!string.IsNullOrEmpty(link))
        {
            responseHeaders.Add("Link", $"<{link}>; rel=\"deprecation\"");
        }

        responseHeaders.Add("Warning", $"299 - \"{message}\"");

        base.OnActionExecuting(context);
    }
}