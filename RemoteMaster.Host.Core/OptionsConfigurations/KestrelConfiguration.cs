// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.OptionsConfigurations;

public class KestrelConfiguration(string commandName, ICertificateLoaderService certificateLoaderService) : IConfigureOptions<KestrelServerOptions>
{
    public void Configure(KestrelServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        switch (commandName)
        {
            case "user":
                options.ListenAnyIP(5001, listenOptions =>
                {
                    listenOptions.UseHttps(certificateLoaderService.GetCurrentCertificate());
                });
                break;
            case "update":
                options.ListenAnyIP(6001, listenOptions =>
                {
                    listenOptions.UseHttps(certificateLoaderService.GetCurrentCertificate());
                });
                break;
            case "chat":
                options.ListenLocalhost(7001);
                break;
            case "service":
                options.ListenLocalhost(7002);
                break;
        }
    }
}