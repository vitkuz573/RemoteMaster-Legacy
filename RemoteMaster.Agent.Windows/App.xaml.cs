// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Core.Extensions;
using RemoteMaster.Agent.Services;
using RemoteMaster.Agent.Windows.Services;

namespace RemoteMaster.Agent;

public partial class App : Application
{
    private IHost _host;

    public IServiceProvider ServiceProvider => _host.Services;

    public App()
    {
        var hostBuilder = CreateDefaultHostBuilder();

        if (WindowsServiceHelpers.IsWindowsService())
        {
            ConfigureAsWindowsService(hostBuilder);
        }
        else
        {
            ConfigureAsWpfApp(hostBuilder);
        }

        _host.StartAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _host?.StopAsync().Wait();
    }

    private static IHostBuilder CreateDefaultHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddCoreServices();
                services.AddSingleton<IClientService, ClientService>();
                services.AddSingleton<IServiceManager, ServiceManager>();
                services.AddSingleton<ISignatureService, SignatureService>();
                services.AddSingleton<IUpdateService, UpdateService>();
                services.AddSingleton<MainWindow>();
            });
    }

    private void ConfigureAsWindowsService(IHostBuilder hostBuilder)
    {
        _host = hostBuilder
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(options => options.ListenAnyIP(3564));
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapCoreHubs());
                });
            })
            .UseWindowsService()
            .Build();

        var updateService = ServiceProvider.GetRequiredService<IUpdateService>();
        updateService.InstallClient();

        MonitorClient();
    }

    private void ConfigureAsWpfApp(IHostBuilder hostBuilder)
    {
        _host = hostBuilder.Build();
        MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    private async void MonitorClient()
    {
        var clientService = ServiceProvider.GetRequiredService<IClientService>();

        while (true)
        {
            if (!clientService.IsClientRunning())
            {
                clientService.StartClient();
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
