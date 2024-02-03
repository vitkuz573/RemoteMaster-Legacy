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
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Host.Windows.Services;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows;

internal class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceMode">Runs the program in service mode, allowing it to operate in the background for continuous service availability.</param>
    /// <param name="userInstance">Launches the program as an instance for the current user.</param>
    /// <param name="install">Performs a silent installation of the program without interactive prompts.</param>
    /// <param name="uninstall">Executes a silent uninstallation of the program without interactive prompts.</param>
    /// <returns></returns>
    private static async Task Main(bool serviceMode = false, bool userInstance = false, bool install = false, bool uninstall = false)
    {
        if (new[] { serviceMode, userInstance, install, uninstall }.Count(val => val) > 1)
        {
            Console.Error.WriteLine("Arguments --install, --uninstall, --service-mode and --user-instance are mutually exclusive. Please specify only one.");

            return;
        }

        var options = new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
        };

        var builder = WebApplication.CreateSlimBuilder(options);
        builder.Host.UseWindowsService();

        builder.Services.AddCoreServices();
        builder.Services.AddSingleton<IUserInstanceService, UserInstanceService>();
        builder.Services.AddSingleton<IHostServiceManager, HostServiceManager>();
        builder.Services.AddSingleton<IServiceManager, ServiceManager>();
        builder.Services.AddSingleton<IServiceConfiguration, HostServiceConfiguration>();
        builder.Services.AddSingleton<IScreenCapturerService, GdiCapturer>();
        builder.Services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        builder.Services.AddSingleton<ICursorRenderService, CursorRenderService>();
        builder.Services.AddSingleton<IInputService, InputService>();
        builder.Services.AddSingleton<IPowerService, PowerService>();
        builder.Services.AddSingleton<IHardwareService, HardwareService>();
        builder.Services.AddSingleton<IUpdaterService, UpdaterService>();
        builder.Services.AddSingleton<ITokenPrivilegeService, TokenPrivilegeService>();
        builder.Services.AddSingleton<IDesktopService, DesktopService>();
        builder.Services.AddSingleton<INetworkDriveService, NetworkDriveService>();
        builder.Services.AddSingleton<IDomainService, DomainService>();
        builder.Services.AddSingleton<IScriptService, ScriptService>();
        builder.Services.AddSingleton<ISecureAttentionSequenceService, SecureAttentionSequenceService>();
        builder.Services.AddSingleton(new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var publicKeyPath = @"C:\ProgramData\RemoteMaster\Security\public_key.pem";

        if (File.Exists(publicKeyPath))
        {
            var publicKey = File.ReadAllText(publicKeyPath);

#pragma warning disable CA2000
            var rsa = RSA.Create();
#pragma warning restore CA2000
            rsa.ImportFromPem(publicKey.ToCharArray());

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = "RemoteMaster Server",
                        ValidAudience = "RMServiceAPI",
                        IssuerSigningKey = new RsaSecurityKey(rsa)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
                            var localIPv6Mapped = IPAddress.Parse("::ffff:127.0.0.1");

                            Log.Information("Incoming request from IP: {Ip}", remoteIp);

                            if (remoteIp != null)
                            {
                                if (remoteIp.Equals(IPAddress.Loopback) || remoteIp.Equals(IPAddress.IPv6Loopback) || remoteIp.Equals(localIPv6Mapped))
                                {
                                    Log.Information("Localhost detected");

                                    var identity = new ClaimsIdentity(new[]
                                    {
                                        new Claim(ClaimTypes.Name, "localhost@localdomain"),
                                    }, "LocalAuth");

                                    context.Principal = new ClaimsPrincipal(identity);
                                    context.Success();
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
        }

        builder.ConfigureSerilog();

        if (!serviceMode)
        {
            builder.ConfigureCoreUrls();
        }
        else
        {
            builder.Services.AddHostedService<MessageLoopService>();
            builder.Services.AddHostedService<HostProcessMonitorService>();
            builder.Services.AddHostedService<CommandListenerService>();
            builder.Services.AddHostedService<HostInfoMonitorService>();
            builder.Services.AddHostedService<HostRegistrationMonitorService>();
        }

        var app = builder.Build();

        if (install)
        {
            var hostConfigurationService = app.Services.GetRequiredService<IHostConfigurationService>();
            var hostInfoService = app.Services.GetRequiredService<IHostInfoService>();
            var hostServiceManager = app.Services.GetRequiredService<IHostServiceManager>();

            HostConfiguration configuration;

            try
            {
                configuration = await hostConfigurationService.LoadConfigurationAsync();
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException)
            {
                Log.Error(ex, "Configuration file not found.");

                return;
            }

            var hostName = hostInfoService.GetHostName();
            var ipv4Address = hostInfoService.GetIPv4Address();
            var macAddress = hostInfoService.GetMacAddress();

            Console.WriteLine(new string('=', 40));
            Console.WriteLine("INSTALLATION DETAILS:");
            Console.WriteLine(new string('-', 40));
            Console.WriteLine($"Server: {configuration.Server}");
            Console.WriteLine($"Group: {configuration.Group}");
            Console.WriteLine(new string('-', 40));
            Console.WriteLine("HOST INFORMATION:");
            Console.WriteLine($"Host Name: {hostName}");
            Console.WriteLine($"IPv4 Address: {ipv4Address}");
            Console.WriteLine($"MAC Address: {macAddress}");
            Console.WriteLine(new string('=', 40));

            await hostServiceManager.InstallOrUpdate(configuration, hostName, ipv4Address, macAddress);

            return;
        }

        if (uninstall)
        {
            var hostConfigurationService = app.Services.GetRequiredService<IHostConfigurationService>();
            var hostInfoService = app.Services.GetRequiredService<IHostInfoService>();
            var hostServiceManager = app.Services.GetRequiredService<IHostServiceManager>();

            HostConfiguration configuration;

            try
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var configurationPath = Path.Combine(programFiles, "RemoteMaster", "Host", hostConfigurationService.ConfigurationFileName);

                configuration = await hostConfigurationService.LoadConfigurationAsync(configurationPath);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException)
            {
                Log.Error(ex, "Configuration error.");

                return;
            }

            await hostServiceManager.Uninstall(configuration);

            return;
        }

        var secureAttentionSequenceService = app.Services.GetRequiredService<ISecureAttentionSequenceService>();

        if (secureAttentionSequenceService.SasOption == SoftwareSasOption.None)
        {
            secureAttentionSequenceService.SasOption = SoftwareSasOption.ServicesAndEaseOfAccessApplications;
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCoreHubs();
        app.Run();
    }
}