// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace RemoteMaster.Server.Core.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureCoreUrls(this WebApplicationBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5076);
        });

        return builder;
    }
}