// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Extensions;

public static class WebApplicationExtensions
{
    public static void UseDynamicHttps(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var certLoaderService = app.Services.GetRequiredService<ICertificateLoaderService>();

        app.UseHttpsRedirection();

        app.Use((context, next) =>
        {
            var httpsConnectionFeature = context.Features.Get<IHttpConnectionFeature>();
            
            if (httpsConnectionFeature != null)
            {
                context.Connection.ClientCertificate = certLoaderService.GetCurrentCertificate();
            }

            return next();
        });
    }
}
