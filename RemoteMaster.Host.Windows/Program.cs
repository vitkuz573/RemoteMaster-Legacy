// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Core.LogEnrichers;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Hubs;
using RemoteMaster.Host.Windows.ScreenOverlays;
using RemoteMaster.Host.Windows.Services;
using RemoteMaster.Host.Windows.WindowsServices;

namespace RemoteMaster.Host.Windows;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var minimalServices = new ServiceCollection();
        ConfigureMinimalServices(minimalServices);
        var minimalServiceProvider = minimalServices.BuildServiceProvider();

        var commandName = args[0];

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Host.UseWindowsService();

        var switchMappings = new Dictionary<string, string>
        {
            { "--srv", "server" },
            { "--server", "server" }
        };

        builder.Configuration.AddCommandLine(args, switchMappings);

        ConfigureServices(builder.Services, commandName);

        var hostInfoEnricher = minimalServiceProvider.GetRequiredService<HostInfoEnricher>();
        var hostConfigurationService = minimalServiceProvider.GetRequiredService<IHostConfigurationService>();
        var certificateLoaderService = minimalServiceProvider.GetRequiredService<ICertificateLoaderService>();

        var serverValue = builder.Configuration["server"];

        await builder.ConfigureSerilog(commandName, commandName == "install" ? serverValue : null, hostConfigurationService, hostInfoEnricher);
        builder.ConfigureCoreUrls(commandName, certificateLoaderService);

        var app = builder.Build();

        var rootCommand = app.Services.ConfigureCommands();
        var parseResult = rootCommand.Parse(args);

        app.Lifetime.ApplicationStarted.Register(Callback);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        if (commandName != "install")
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        app.MapCoreHubs(commandName);

        if (commandName == "user")
        {
            app.MapHub<ServiceHub>("/hubs/service");
            app.MapHub<DeviceManagerHub>("/hubs/devicemanager");
            app.MapHub<RegistryHub>("/hubs/registry");
        }

        await app.RunAsync();

        async void Callback()
        {
            var exitCode = await parseResult.InvokeAsync();
        }
    }

    private static void ConfigureMinimalServices(IServiceCollection services)
    {
        services.AddMinimalCoreServices();        
    }

    private static void ConfigureServices(IServiceCollection services, string commandName)
    {
        services.AddHttpContextAccessor();

        services.AddCoreServices(commandName);
        
        services.AddTransient<IRegistryKeyFactory, RegistryKeyFactory>();
        services.AddTransient<INativeProcessFactory, NativeProcessFactory>();
        services.AddTransient<IShellScriptHandlerFactory, ShellScriptHandlerFactory>();
        services.AddSingleton<IService, HostService>();
        services.AddSingleton<IUserInstanceService, UserInstanceService>();
        services.AddSingleton<IScreenCapturingService, GdiCapturing>();
        services.AddSingleton<IInputService, InputService>();
        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<IHardwareService, HardwareService>();
        services.AddSingleton<ITokenPrivilegeService, TokenPrivilegeService>();
        services.AddSingleton<IDesktopService, DesktopService>();
        services.AddSingleton<INetworkDriveService, NetworkDriveService>();
        services.AddSingleton<IDomainService, DomainService>();
        services.AddSingleton<IScriptService, ScriptService>();
        services.AddSingleton<ITaskManagerService, TaskManagerService>();
        services.AddSingleton<ISecureAttentionSequenceService, SecureAttentionSequenceService>();
        services.AddSingleton<IPsExecService, PsExecService>();
        services.AddSingleton<IFirewallService, FirewallService>();
        services.AddSingleton<IWoLConfiguratorService, WoLConfiguratorService>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IWorkStationSecurityService, WorkStationSecurityService>();
        services.AddSingleton<ICommandExecutor, CommandExecutor>();
        services.AddSingleton<IDeviceManagerService, DeviceManagerService>();
        services.AddSingleton<IOperatingSystemInformationService, OperatingSystemInformationService>();
        services.AddSingleton<ITrayIconManager, TrayIconManager>();
        services.AddSingleton<ICommandLineProvider, CommandLineProvider>();
        services.AddSingleton<ISessionService, SessionService>();

        services.AddSingleton<IScreenOverlay, CursorOverlay>();

        switch (commandName)
        {
            case "user":
                services.AddHostedService<WoLInitializationService>();
                services.AddHostedService<FirewallInitializationService>();
                services.AddHostedService<SasInitializationService>();
                services.AddHostedService<TrayIconHostedService>();
                break;
            case "service":
                services.AddHostedService<CommandListenerService>();
                services.AddHostedService<MessageLoopService>();
                break;
            case "chat":
                services.AddHostedService<ChatWindowService>();
                break;
        }
    }
}
