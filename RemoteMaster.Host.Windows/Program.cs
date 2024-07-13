// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.AuthorizationHandlers;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Core.Requirements;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Enums;
using RemoteMaster.Host.Windows.Helpers;
using RemoteMaster.Host.Windows.Hubs;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var launchModeInstance = ParseArguments(args);

        if (launchModeInstance is UpdaterMode updaterMode && string.IsNullOrEmpty(updaterMode.Parameters["folder-path"].Value))
        {
            PrintHelp(launchModeInstance);
            return;
        }

        var options = new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory
        };

        var builder = WebApplication.CreateSlimBuilder(options);

        builder.Configuration.AddCommandLine(args);

        if (launchModeInstance is ServiceMode)
        {
            builder.Host.UseWindowsService();
        }

        ConfigureServices(builder.Services, launchModeInstance);

        builder.ConfigureSerilog();

        if (launchModeInstance != null)
        {
            builder.ConfigureCoreUrls(launchModeInstance);
        }

        var app = builder.Build();

        switch (launchModeInstance)
        {
            case InstallMode:
                var hostInstaller = app.Services.GetRequiredService<IHostInstaller>();
                await hostInstaller.InstallAsync();
                return;
            case UninstallMode:
                var hostUninstaller = app.Services.GetRequiredService<IHostUninstaller>();
                await hostUninstaller.UninstallAsync();
                return;
        }

        if (launchModeInstance != null)
        {
            var secureAttentionSequenceService = app.Services.GetRequiredService<ISecureAttentionSequenceService>();

            if (secureAttentionSequenceService.SasOption != SoftwareSasOption.ServicesAndEaseOfAccessApplications)
            {
                secureAttentionSequenceService.SasOption = SoftwareSasOption.ServicesAndEaseOfAccessApplications;
            }

            var firewallService = app.Services.GetRequiredService<IFirewallService>();

            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var hostRootPath = Path.Combine(programFilesPath, "RemoteMaster", "Host");
            var hostApplicationPath = Path.Combine(hostRootPath, "RemoteMaster.Host.exe");
            var hostUpdaterApplicationPath = Path.Combine(hostRootPath, "Updater", "RemoteMaster.Host.exe");

            firewallService.AddRule("Remote Master Host", hostApplicationPath);
            firewallService.AddRule("Remote Master Host Updater", hostUpdaterApplicationPath);

            var wolConfiguratorService = app.Services.GetRequiredService<IWoLConfiguratorService>();

            wolConfiguratorService.DisableFastStartup();
            wolConfiguratorService.DisablePnPEnergySaving();
            await wolConfiguratorService.EnableWakeOnLanForAllAdaptersAsync();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseAuthentication();
        app.UseAuthorization();

        if (launchModeInstance != null)
        {
            app.MapCoreHubs(launchModeInstance);
        }

        if (launchModeInstance is UserMode)
        {
            app.MapHub<ServiceHub>("/hubs/service");
        }

        await app.RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services, LaunchModeBase launchModeInstance)
    {
        services.AddHttpContextAccessor();

        if (launchModeInstance != null)
        {
            services.AddCoreServices(launchModeInstance);
        }

        services.AddTransient<IServiceFactory, ServiceFactory>();
        services.AddTransient<IRegistryKeyFactory, RegistryKeyFactory>();
        services.AddTransient<IProcessWrapperFactory, ProcessWrapperFactory>();
        services.AddTransient<INativeProcessFactory, NativeProcessFactory>();
        services.AddSingleton<IUserInstanceService, UserInstanceService>();
        services.AddSingleton<IUpdaterInstanceService, UpdaterInstanceService>();
        services.AddSingleton<IHostInstaller, HostInstaller>();
        services.AddSingleton<IHostUninstaller, HostUninstaller>();
        services.AddSingleton<IScreenCapturerService, GdiCapturer>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        services.AddSingleton<ICursorRenderService, CursorRenderService>();
        services.AddSingleton<IInputService, InputService>();
        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<IHardwareService, HardwareService>();
        services.AddSingleton<IHostUpdater, HostUpdater>();
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
        services.AddSingleton<IProcessService, ProcessService>();
        services.AddSingleton<ISessionChangeEventService, SessionChangeEventService>();
        services.AddSingleton<IArgumentBuilderService, ArgumentBuilderService>();
        services.AddSingleton<IInstanceStarterService, InstanceStarterService>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IWorkStationSecurityService, WorkStationSecurityService>();
        services.AddSingleton<IAuthorizationHandler, LocalhostOrAuthenticatedHandler>();
        services.AddSingleton(new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");

        if (File.Exists(publicKeyPath))
        {
            var publicKey = File.ReadAllBytesAsync(publicKeyPath).Result;

#pragma warning disable CA2000
            var rsa = RSA.Create();
#pragma warning restore CA2000
            rsa.ImportRSAPublicKey(publicKey, out _);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwtBearerOptions =>
                {
                    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = "RemoteMaster Server",
                        ValidAudience = "RMServiceAPI",
                        IssuerSigningKey = new RsaSecurityKey(rsa),
                        RoleClaimType = ClaimTypes.Role,
                        AuthenticationType = "JWT Security"
                    };
                });

            services.AddAuthorizationBuilder()
                .AddPolicy("LocalhostOrAuthenticatedPolicy", policy =>
                    policy.Requirements.Add(new LocalhostOrAuthenticatedRequirement()))
                .AddPolicy("MouseInputPolicy", policy =>
                    policy.RequireClaim("Permission", "MouseInput"))
                .AddPolicy("KeyboardInputPolicy", policy =>
                    policy.RequireClaim("Permission", "KeyboardInput"))
                .AddPolicy("SwitchScreenPolicy", policy =>
                    policy.RequireClaim("Permission", "SwitchScreen"))
                .AddPolicy("ToggleInputPolicy", policy =>
                    policy.RequireClaim("Permission", "ToggleInput"))
                .AddPolicy("ToggleUserInputPolicy", policy =>
                    policy.RequireClaim("Permission", "ToggleUserInput"))
                .AddPolicy("ChangeImageQualityPolicy", policy =>
                    policy.RequireClaim("Permission", "ChangeImageQuality"))
                .AddPolicy("ToggleCursorTrackingPolicy", policy =>
                    policy.RequireClaim("Permission", "ToggleCursorTracking"))
                .AddPolicy("TerminateHostPolicy", policy =>
                    policy.RequireClaim("Permission", "TerminateHost"))
                .AddPolicy("RebootComputerPolicy", policy =>
                    policy.RequireClaim("Permission", "RebootComputer"))
                .AddPolicy("ShutdownComputerPolicy", policy =>
                    policy.RequireClaim("Permission", "ShutdownComputer"))
                .AddPolicy("ChangeMonitorStatePolicy", policy =>
                    policy.RequireClaim("Permission", "ChangeMonitorState"))
                .AddPolicy("ExecuteScriptPolicy", policy =>
                    policy.RequireClaim("Permission", "ExecuteScript"))
                .AddPolicy("LockWorkStationPolicy", policy =>
                    policy.RequireClaim("Permission", "LockWorkStation"))
                .AddPolicy("LogOffUserPolicy", policy =>
                    policy.RequireClaim("Permission", "LogOffUser"))
                .AddPolicy("MovePolicy", policy =>
                    policy.RequireClaim("Permission", "MoveHost"))
                .AddPolicy("RenewCertificatePolicy", policy =>
                    policy.RequireClaim("Permission", "RenewCertificate"));
        }

        switch (launchModeInstance)
        {
            case UserMode:
                services.AddHostedService<InputBackgroundService>();
                break;
            case ServiceMode:
                services.AddHostedService<CertificateManagementService>();
                services.AddHostedService<HostProcessMonitorService>();
                services.AddHostedService<HostRegistrationMonitorService>();
                services.AddHostedService<MessageLoopService>();
                services.AddHostedService<CommandListenerService>();
                break;
            case UpdaterMode:
                services.AddHostedService<UpdaterBackground>();
                break;
        }
    }

    private static LaunchModeBase? ParseArguments(string[] args)
    {
        var helpRequested = args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase));
        var modeArgument = args.FirstOrDefault(arg => arg.StartsWith("--launch-mode="))?.Split('=')[1];

        if (args.Length == 0 || (helpRequested && string.IsNullOrEmpty(modeArgument)))
        {
            PrintHelp(null);
            Environment.Exit(0);
        }

        var assembly = Assembly.GetAssembly(typeof(LaunchModeBase));

        if (assembly == null)
        {
            return null;
        }

        var launchModes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(LaunchModeBase)))
            .ToArray();

        var launchModeType = launchModes.FirstOrDefault(t => string.Equals(t.Name, $"{modeArgument}Mode", StringComparison.OrdinalIgnoreCase));

        if (launchModeType == null)
        {
            if (modeArgument is null)
            {
                PrintHelp(null);
            }
            else
            {
                SuggestSimilarModes(modeArgument, launchModes);
            }

            Environment.Exit(1);
        }

        var launchModeInstance = (LaunchModeBase?)Activator.CreateInstance(launchModeType);

        if (launchModeInstance == null)
        {
            return null;
        }

        foreach (var arg in args)
        {
            if (arg.StartsWith("--"))
            {
                var equalIndex = arg.IndexOf('=');
                string key;
                var value = "";

                if (equalIndex >= 0)
                {
                    key = arg[2..equalIndex];
                    value = arg[(equalIndex + 1)..];
                }
                else
                {
                    key = arg[2..];
                    value = "true";
                }

                if (launchModeInstance.Parameters.ContainsKey(key))
                {
                    launchModeInstance.Parameters[key].Value = value;
                }
            }
        }

        if (helpRequested)
        {
            PrintHelp(launchModeInstance);
            Environment.Exit(0);
        }

        return launchModeInstance;
    }

    private static void SuggestSimilarModes(string inputMode, Type[] availableModes)
    {
        if (string.IsNullOrEmpty(inputMode))
        {
            Console.WriteLine("No launch mode provided.");
            return;
        }

        var modeNames = availableModes.Select(m => m.Name.Replace("Mode", "", StringComparison.OrdinalIgnoreCase));
        var suggestions = modeNames.Select(name => new
        {
            Name = name,
            Distance = LevenshteinDistanceUtility.ComputeLevenshteinDistance(inputMode.ToLower(), name.ToLower()) // Используем новый утилитарный класс
        })
            .OrderBy(x => x.Distance)
            .Take(3);

        Console.WriteLine("Did you mean one of these modes?");

        foreach (var suggestion in suggestions)
        {
            Console.WriteLine($"- {suggestion.Name}");
        }
    }

    private static void PrintHelp(LaunchModeBase? specificMode)
    {
        if (specificMode != null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{specificMode.Name} Mode Options:");
            Console.ResetColor();

            Console.WriteLine($"  {specificMode.Description}");

            Console.WriteLine();

            foreach (var param in specificMode.Parameters)
            {
                Console.WriteLine($"  --{param.Key}: {param.Value.Description} {(param.Value.IsRequired ? "(Required)" : "(Optional)")}");
            }
        }
        else
        {
            var assembly = Assembly.GetAssembly(typeof(LaunchModeBase));

            if (assembly == null)
            {
                return;
            }

            var launchModes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(LaunchModeBase)))
                .Select(t => Activator.CreateInstance(t) as LaunchModeBase)
                .Where(instance => instance != null);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name} [OPTIONS]");
            Console.ResetColor();
            Console.WriteLine();

            foreach (var mode in launchModes)
            {
                if (mode != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{mode.Name} Mode:");
                    Console.ResetColor();

                    Console.WriteLine($"  {mode.Description}");
                    Console.WriteLine();
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Use \"--help --launch-mode=<MODE>\" for more details on a specific mode.");
            Console.ResetColor();
        }
    }
}
