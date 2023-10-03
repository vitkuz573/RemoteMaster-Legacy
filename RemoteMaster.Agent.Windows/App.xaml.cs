// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Core.Extensions;
using RemoteMaster.Agent.Models;
using RemoteMaster.Agent.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Agent;

public partial class App : Application
{
    private IHost _host;
    private readonly HiddenWindow _hiddenWindow;

    protected const string SharedFolder = @"\\SERVER-DC02\Win\RemoteMaster";
    protected const string Login = "support@it-ktk.local";
    protected const string Password = "bonesgamer123!!";

    public IServiceProvider ServiceProvider => _host.Services;

    public App()
    {
        var args = Environment.GetCommandLineArgs();

        var hostBuilder = CreateDefaultHostBuilder(args);

        if (WindowsServiceHelpers.IsWindowsService())
        {
            ConfigureAsWindowsService(hostBuilder);

            _hiddenWindow = new HiddenWindow();
            _hiddenWindow.Show();
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

    private static IHostBuilder CreateDefaultHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddCoreServices();
                services.AddSingleton<IClientService, ClientService>();
                services.AddSingleton<IAgentServiceManager, AgentServiceManager>();
                services.AddSingleton<IUpdaterServiceManager, UpdaterServiceManager>();
                services.AddSingleton<IServiceManager, ServiceManager>();
                services.AddSingleton<ISignatureService, SignatureService>();
                services.AddSingleton<MainWindow>();

                services.AddSingleton<AgentServiceConfig>();
                services.AddSingleton<UpdaterServiceConfig>();

                services.AddSingleton<IDictionary<string, IServiceConfig>>(sp => new Dictionary<string, IServiceConfig>
                {
                    { "agent", sp.GetRequiredService<AgentServiceConfig>() },
                    { "updater", sp.GetRequiredService<UpdaterServiceConfig>() }
                });
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

        var sourceFolder = Path.Combine(SharedFolder, "Client");
        var destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Client");

        _ = TryExecuteWithRetryAsync(async () =>
        {
            try
            {
                NetworkDriveHelper.MapNetworkDrive(SharedFolder, Login, Password);
                NetworkDriveHelper.DirectoryCopy(sourceFolder, destinationFolder);
                NetworkDriveHelper.CancelNetworkDrive(SharedFolder);

                return true;
            }
            catch
            {
                return false;
            }
        });

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
            if (!clientService.IsRunning())
            {
                clientService.Start();
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private static async Task<bool> TryExecuteWithRetryAsync(Func<Task<bool>> operation)
    {
        while (true)
        {
            try
            {
                if (await operation())
                {
                    return true;
                }
            }
            catch { }

            await Task.Delay(10000);
        }
    }
}