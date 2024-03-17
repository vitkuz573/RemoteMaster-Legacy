// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Hubs;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var launchArguments = ParseArguments(args);

        if (launchArguments.HelpRequested || launchArguments.LaunchMode == LaunchMode.Default)
        {
            PrintHelp(launchArguments.LaunchMode);
            return;
        }

        if (launchArguments.LaunchMode == LaunchMode.Updater && string.IsNullOrEmpty(launchArguments.FolderPath))
        {
            Console.WriteLine("Error: The --folder-path option must be specified for the updater mode (--launch-mode=update).");
            return;
        }

        var options = new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
        };

        var builder = WebApplication.CreateSlimBuilder(options);
        builder.Host.UseWindowsService();

        builder.Services.AddCoreServices(launchArguments.LaunchMode);
        builder.Services.AddTransient<IServiceFactory, ServiceFactory>();
        builder.Services.AddSingleton<IUserInstanceService, UserInstanceService>();
        builder.Services.AddSingleton<IUpdaterInstanceService, UpdaterInstanceService>();
        builder.Services.AddSingleton<IHostInstaller, HostInstaller>();
        builder.Services.AddSingleton<IHostUninstaller, HostUninstaller>();
        builder.Services.AddSingleton<IScreenCapturerService, GdiCapturer>();
        builder.Services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        builder.Services.AddSingleton<ICursorRenderService, CursorRenderService>();
        builder.Services.AddSingleton<IInputService, InputService>();
        builder.Services.AddSingleton<IPowerService, PowerService>();
        builder.Services.AddSingleton<IHardwareService, HardwareService>();
        builder.Services.AddSingleton<IHostUpdater, HostUpdater>();
        builder.Services.AddSingleton<ITokenPrivilegeService, TokenPrivilegeService>();
        builder.Services.AddSingleton<IDesktopService, DesktopService>();
        builder.Services.AddSingleton<INetworkDriveService, NetworkDriveService>();
        builder.Services.AddSingleton<IDomainService, DomainService>();
        builder.Services.AddSingleton<IScriptService, ScriptService>();
        builder.Services.AddSingleton<ITaskManagerService, TaskManagerService>();
        builder.Services.AddSingleton<ISecureAttentionSequenceService, SecureAttentionSequenceService>();
        builder.Services.AddSingleton<IPsExecService, PsExecService>();
        builder.Services.AddSingleton<IFirewallSettingService, FirewallSettingService>();
        builder.Services.AddSingleton(new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programData, "RemoteMaster", "Security", "public_key.pem");

        if (File.Exists(publicKeyPath))
        {
            var publicKey = await File.ReadAllTextAsync(publicKeyPath);

#pragma warning disable CA2000
            var rsa = RSA.Create();
#pragma warning restore CA2000
            rsa.ImportFromPem(publicKey.ToCharArray());

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                        IssuerSigningKey = new RsaSecurityKey(rsa)
                    };

                    jwtBearerOptions.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
                            var localIPv6Mapped = IPAddress.Parse("::ffff:127.0.0.1");

                            if (remoteIp == null)
                            {
                                return Task.CompletedTask;
                            }

                            if (!remoteIp.Equals(IPAddress.Loopback) && !remoteIp.Equals(IPAddress.IPv6Loopback) && !remoteIp.Equals(localIPv6Mapped))
                            {
                                return Task.CompletedTask;
                            }

                            var identity = new ClaimsIdentity(
                            [
                                new Claim(ClaimTypes.Name, "localhost@localdomain"),
                            ], "LocalAuth");

                            context.Principal = new ClaimsPrincipal(identity);
                            context.Success();

                            return Task.CompletedTask;
                        }
                    };
                });
        }

        builder.ConfigureSerilog();

        switch (launchArguments.LaunchMode)
        {
            case LaunchMode.User:
                builder.ConfigureCoreUrls();
                break;
            case LaunchMode.Service:
                builder.Services.AddHostedService<HostProcessMonitorService>();
                builder.Services.AddHostedService<HostRegistrationMonitorService>();
                builder.Services.AddHostedService<MessageLoopService>();
                builder.Services.AddHostedService<CommandListenerService>();
                break;
        }

        var app = builder.Build();

        switch (launchArguments.LaunchMode)
        {
            case LaunchMode.Install:
            {
                var hostInstallerService = app.Services.GetRequiredService<IHostInstaller>();
                await hostInstallerService.InstallAsync();

                return;
            }
            case LaunchMode.Uninstall:
            {
                var hostUninstallerService = app.Services.GetRequiredService<IHostUninstaller>();
                await hostUninstallerService.UninstallAsync();

                return;
            }
        }

        var secureAttentionSequenceService = app.Services.GetRequiredService<ISecureAttentionSequenceService>();

        if (secureAttentionSequenceService.SasOption == SoftwareSasOption.None)
        {
            secureAttentionSequenceService.SasOption = SoftwareSasOption.ServicesAndEaseOfAccessApplications;
        }

        var firewallSettingService = app.Services.GetRequiredService<IFirewallSettingService>();

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var applicationPath = Path.Combine(programFiles, "RemoteMaster", "Host", "RemoteMaster.Host.exe");
        firewallSettingService.Execute("Remote Master Host", applicationPath);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseAuthentication();
        app.UseAuthorization();

        if (launchArguments.LaunchMode is LaunchMode.User)
        {
            app.MapCoreHubs();
            app.MapHub<ServiceHub>("/hubs/service");
        }

        if (launchArguments.LaunchMode is LaunchMode.Updater)
        {
            var hostUpdater = app.Services.GetRequiredService<IHostUpdater>();

            await hostUpdater.UpdateAsync(launchArguments.FolderPath, launchArguments.Username, launchArguments.Password, launchArguments.ForceUpdate, launchArguments.AllowDowngrade);
        }
        else
        {
            await app.RunAsync();
        }
    }

    private static LaunchArguments ParseArguments(string[] args)
    {
        var launchArguments = new LaunchArguments
        {
            HelpRequested = args.Contains("--help")
        };

        foreach (var arg in args)
        {
            if (arg.StartsWith("--launch-mode="))
            {
                var modeString = arg["--launch-mode=".Length..];

                if (Enum.TryParse(modeString, true, out LaunchMode launchMode) && Enum.IsDefined(typeof(LaunchMode), launchMode))
                {
                    launchArguments.LaunchMode = launchMode;
                }
                else
                {
                    Console.WriteLine($"Error: '{modeString}' is not a valid launch mode.");
                    PrintHelp();
                    Environment.Exit(1);
                }
            }
            else if (arg.StartsWith("--folder-path="))
            {
                launchArguments.FolderPath = arg["--folder-path=".Length..];
            }
            else if (arg.StartsWith("--username="))
            {
                launchArguments.Username = arg["--username=".Length..];
            }
            else if (arg.StartsWith("--password="))
            {
                launchArguments.Password = arg["--password=".Length..];
            }
            else if (arg.Equals("--force-update"))
            {
                launchArguments.ForceUpdate = true;
            }
            else if (arg.Equals("--allow-downgrade"))
            {
                launchArguments.AllowDowngrade = true;
            }
        }

        return launchArguments;
    }

    private static void PrintHelp(LaunchMode launchMode = LaunchMode.Default)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Usage: RemoteMaster.Host [OPTIONS]");
        Console.ResetColor();

        Console.WriteLine();

        if (launchMode == LaunchMode.Updater)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Updater Mode Options:");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  --folder-path=PATH\tSpecifies the folder path for the update operation. Required.");
            Console.WriteLine("  --username=USERNAME\tSpecifies the username for authentication. Optional.");
            Console.WriteLine("  --password=PASSWORD\tSpecifies the password for authentication. Optional.");
            Console.WriteLine("  --force-update\tForces the update operation to proceed, even if no update is needed. Optional.");
            Console.WriteLine("  --allow-downgrade\tAllows the update operation to proceed with a lower version than the current one. Optional.");
            Console.ResetColor();

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Example:");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("  RemoteMaster.Host --launch-mode=Updater --folder-path=\"C:\\UpdateFolder\" --force-update --allow-downgrade");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Options:");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  --help\t\tDisplays this help message and exits.");
            Console.WriteLine("  --launch-mode=MODE\tSpecifies the launch mode of the program. Available modes:");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\t\t\tUser - Runs the program in user mode.");
            Console.WriteLine("\t\t\tService - Runs the program as a service.");
            Console.WriteLine("\t\t\tInstall - Installs the necessary components for the program.");
            Console.WriteLine("\t\t\tUninstall - Removes the program and its components.");
            Console.WriteLine("\t\t\tUpdater - Updates the program to the latest version.");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Examples:");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("  RemoteMaster.Host --launch-mode=Service");
            Console.WriteLine("  RemoteMaster.Host --help");
            Console.ResetColor();
        }
    }
}