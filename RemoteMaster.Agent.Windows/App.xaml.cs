// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO;
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
using RemoteMaster.Agent.Helpers.AdvFirewall;
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
        var hostBuilder = CreateDefaultHostBuilder();

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

    private static IHostBuilder CreateDefaultHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddCoreServices();
                services.AddSingleton<IClientService, ClientService>();
                services.AddSingleton<IAgentServiceManager, AgentServiceManager>();
                services.AddSingleton<IUpdaterServiceManager, UpdaterServiceManager>();
                services.AddSingleton<AgentServiceConfigProvider>();
                services.AddSingleton<UpdaterServiceConfigProvider>();
                services.AddSingleton<IServiceManager, ServiceManager>();
                services.AddSingleton<ISignatureService, SignatureService>();
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

        var sourceFolder = Path.Combine(SharedFolder, "Client");
        var destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Client");

        NetworkDriveHelper.MapNetworkDrive(SharedFolder, Login, Password);
        NetworkDriveHelper.DirectoryCopy(sourceFolder, destinationFolder);
        NetworkDriveHelper.CancelNetworkDrive(SharedFolder);

        // // Удалить существующее правило
        // FirewallManager.DeleteRule("PSExec", RuleDirection.In);
        // 
        // // Отключить группу правил
        // FirewallManager.SetRuleGroup("Удаленное управление службой", RuleGroupStatus.Disabled);
        // 
        // // Включить WinRM
        // FirewallManager.EnableWinRM();
        // 
        // // Добавить новое правило брандмауэра
        // var rule = new FirewallRule("PSExec")
        // {
        //     Direction = RuleDirection.In,
        //     Action = RuleAction.Allow,
        //     Protocol = RuleProtocol.TCP,
        //     LocalPort = "RPC",
        //     Program = @"%WinDir%\system32\services.exe",
        //     Service = "any"
        // };
        // 
        // rule.Profiles.Add(RuleProfile.Domain);
        // rule.Profiles.Add(RuleProfile.Private);
        // rule.Apply();
        // 
        // // Включить группу правил
        // FirewallManager.SetRuleGroup("Удаленное управление службой", RuleGroupStatus.Enabled);

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
}