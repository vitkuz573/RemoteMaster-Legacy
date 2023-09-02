// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Core.Extensions;
using RemoteMaster.Agent.Core.Models;
using RemoteMaster.Agent.Services;

namespace RemoteMaster.Agent;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        if (WindowsServiceHelpers.IsWindowsService())
        {
            _host = Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddCoreServices(hostContext.Configuration);
                    services.AddSingleton<ISignatureService, SignatureService>();
                    services.AddSingleton<IProcessService, ProcessService>();
                })
                .UseWindowsService()
                .Build();

            _host.StartAsync();

            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            var serverSettings = _host.Services.GetRequiredService<IOptions<ServerSettings>>().Value;

            logger.LogInformation("Client settings: Path = {Path}, CertificateThumbprint = {Thumbprint}", serverSettings.Path, serverSettings.CertificateThumbprint);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        _host?.StopAsync();
    }
}
