// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace RemoteMaster.Server.Validators;

public class ValidateMediaTypeAttribute : ActionFilterAttribute
{
    private readonly List<string> _supportedMediaTypes =
    [
        "application/vnd.remotemaster.v1+json"
    ];

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.HttpContext.Request.Headers.TryGetValue(HeaderNames.Accept, out var acceptHeader))
        {
            var mediaType = acceptHeader.FirstOrDefault();

            if (!_supportedMediaTypes.Contains(mediaType))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                context.Result = new Microsoft.AspNetCore.Mvc.JsonResult(new
                {
                    Status = StatusCodes.Status415UnsupportedMediaType,
                    Message = $"The media type '{mediaType}' is not supported."
                });

                return;
            }
        }

        base.OnActionExecuting(context);
    }
}