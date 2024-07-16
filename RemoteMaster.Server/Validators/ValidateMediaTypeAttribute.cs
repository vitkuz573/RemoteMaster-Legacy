// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    Status = StatusCodes.Status415UnsupportedMediaType,
                    Message = $"The media type '{mediaType}' is not supported."
                };

                var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                context.HttpContext.Response.WriteAsync(jsonResponse);
                context.Result = new EmptyResult();

                return;
            }
        }

        base.OnActionExecuting(context);
    }
}