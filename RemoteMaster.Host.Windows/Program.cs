// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Reflection;
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
        var launchModeInstance = ParseArguments(args);

        if (launchModeInstance is UpdaterMode updaterMode && string.IsNullOrEmpty(updaterMode.Parameters["folder-path"].Value))
        {
            PrintHelp(launchModeInstance);
            return;
        }

        var options = new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
        };

        var builder = WebApplication.CreateSlimBuilder(options);
        builder.Host.UseWindowsService();

        builder.Services.AddCoreServices(launchModeInstance);
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

        switch (launchModeInstance)
        {
            case UserMode:
                builder.ConfigureCoreUrls();
                break;
            case ServiceMode:
                builder.Services.AddHostedService<HostProcessMonitorService>();
                builder.Services.AddHostedService<HostRegistrationMonitorService>();
                builder.Services.AddHostedService<MessageLoopService>();
                builder.Services.AddHostedService<CommandListenerService>();
                break;
        }

        var app = builder.Build();

        switch (launchModeInstance)
        {
            case InstallMode:
            {
                var hostInstaller = app.Services.GetRequiredService<IHostInstaller>();
                await hostInstaller.InstallAsync();

                return;
            }
            case UninstallMode:
            {
                var hostUninstaller = app.Services.GetRequiredService<IHostUninstaller>();
                await hostUninstaller.UninstallAsync();

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

        if (launchModeInstance is UserMode userMode)
        {
            app.MapCoreHubs();
            app.MapHub<ServiceHub>("/hubs/service");
        }

        if (launchModeInstance is UpdaterMode updateMode)
        {
            var hostUpdater = app.Services.GetRequiredService<IHostUpdater>();

            var folderPath = updateMode.Parameters["folder-path"].Value ?? throw new InvalidOperationException("Folder path is required.");
            var username = updateMode.Parameters["username"].Value;
            var password = updateMode.Parameters["password"].Value;
            var force = updateMode.Parameters["force"].Value?.ToLower() == "true";
            var allowDowngrade = updateMode.Parameters["allow-downgrade"].Value?.ToLower() == "true";

            await hostUpdater.UpdateAsync(folderPath, username, password, force, allowDowngrade);
        }
        else
        {
            await app.RunAsync();
        }
    }

    private static LaunchModeBase ParseArguments(string[] args)
    {
        var helpRequested = args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase));
        var modeArgument = args.FirstOrDefault(arg => arg.StartsWith("--launch-mode="))?.Split('=')[1];

        if (args.Length == 0 || (helpRequested && string.IsNullOrEmpty(modeArgument)))
        {
            PrintHelp(null);
            Environment.Exit(0);
        }

        var assembly = Assembly.GetAssembly(typeof(LaunchModeBase));
        var launchModes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(LaunchModeBase)))
            .ToArray();

        var launchModeType = launchModes.FirstOrDefault(t => string.Equals(t.Name, modeArgument + "Mode", StringComparison.OrdinalIgnoreCase));

        if (launchModeType == null)
        {
            Console.WriteLine($"Error: Unrecognized launch mode '{modeArgument}'.");
            SuggestSimilarModes(modeArgument, launchModes);
            Environment.Exit(1);
        }

        var launchModeInstance = (LaunchModeBase)Activator.CreateInstance(launchModeType);

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
        var modeNames = availableModes.Select(m => m.Name.Replace("Mode", "", StringComparison.OrdinalIgnoreCase));
        var suggestions = modeNames.Select(name => new
        {
            Name = name,
            Distance = ComputeLevenshteinDistance(inputMode.ToLower(), name.ToLower())
        })
        .OrderBy(x => x.Distance)
        .Take(3);

        Console.WriteLine("Did you mean one of these modes?");

        foreach (var suggestion in suggestions)
        {
            Console.WriteLine($"- {suggestion.Name}");
        }
    }

    private static int ComputeLevenshteinDistance(string source1, string source2)
    {
        var matrix = new int[source1.Length + 1, source2.Length + 1];

        for (var i = 0; i <= source1.Length; matrix[i, 0] = i++) { }
        for (var j = 0; j <= source2.Length; matrix[0, j] = j++) { }

        for (var i = 1; i <= source1.Length; i++)
        {
            for (var j = 1; j <= source2.Length; j++)
            {
                var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source1.Length, source2.Length];
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
            var launchModes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(LaunchModeBase)))
                .Select(t => Activator.CreateInstance(t) as LaunchModeBase)
                .Where(instance => instance != null);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Usage: RemoteMaster.Host [OPTIONS]");
            Console.ResetColor();
            Console.WriteLine();

            foreach (var mode in launchModes)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{mode.Name} Mode:");
                Console.ResetColor();

                Console.WriteLine($"  {mode.Description}");
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Use \"--help --launch-mode=<MODE>\" for more details on a specific mode.");
            Console.ResetColor();
        }
    }
}