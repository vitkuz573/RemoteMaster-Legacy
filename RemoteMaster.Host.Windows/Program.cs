// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Extensions;
using RemoteMaster.Host.Windows.Hubs;
using RemoteMaster.Host.Windows.ScreenOverlays;
using RemoteMaster.Host.Windows.Services;
using RemoteMaster.Host.Windows.WindowsServices;
using Serilog;

namespace RemoteMaster.Host.Windows;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Host.UseWindowsService();
        builder.Host.UseSerilog();

        builder.Configuration.AddCommandLine(args);

        string? commandName = null;

        if (args.Length > 0 && !args[0].StartsWith('-'))
        {
            commandName = args[0].ToLower();
        }

        ConfigureServices(builder.Services, commandName);

        var app = builder.Build();

        var rootCommand = app.Services.ConfigureCommands();

        var parseResult = rootCommand.Parse(args);
        var commandResult = parseResult.CommandResult;

        string? server = null;

        if (commandResult.Command.Name.Equals("install", StringComparison.OrdinalIgnoreCase))
        {
            var installCommand = rootCommand.Subcommands.First(c => c.Name == "install");
            var serverOption = installCommand.Options.First(o => o.Name == "--server");

            server = parseResult.GetValue<string>(serverOption.Name);
        }

        app.MapCoreHubs(commandName);

        if (commandName == "user")
        {
            app.MapHub<ServiceHub>("/hubs/service");
            app.MapHub<DeviceManagerHub>("/hubs/devicemanager");
            app.MapHub<RegistryHub>("/hubs/registry");
        }

        await app.ConfigureSerilog(server);

        var oneOffCommands = new HashSet<string> { "install", "update", "uninstall", "reinstall" };

        var shouldInvoke = args.Length == 0 ||
                           args[0].StartsWith('-') ||
                           (commandResult.Command != rootCommand && oneOffCommands.Contains(commandResult.Command.Name.ToLower()));

        var isUpdateCommand = commandName == "update";

        if (isUpdateCommand)
        {
            app.Lifetime.ApplicationStarted.Register(Callback);
        }
        else if (shouldInvoke)
        {
            var exitCode = await parseResult.InvokeAsync();

            Environment.Exit(exitCode);
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        if (commandName != "install")
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        await app.RunAsync();

        return;

        async void Callback()
        {
            var exitCode = await parseResult.InvokeAsync();

            Environment.Exit(exitCode);
        }
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
        services.AddSingleton<IAudioCapturingService, AudioCapturingService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IScreenProvider, ScreenProvider>();
        services.AddSingleton<IScreenCastingService, ScreenCastingService>();

        services.AddSingleton<IScreenOverlay, CursorOverlay>();

        if (commandName != "install")
        {
            services.AddAuthorizationBuilder()
                .AddCoreRequirements()
                .AddCorePolicies()
                .AddWindowsPolicies();
        }

        switch (commandName)
        {
            case "user":
                services.AddHostedService<WoLInitializationService>();
                services.AddHostedService<FirewallInitializationService>();
                services.AddHostedService<SasInitializationService>();
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
