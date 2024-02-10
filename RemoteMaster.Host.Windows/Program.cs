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
            await Console.Error.WriteLineAsync("Arguments --install, --uninstall, --service-mode and --user-instance are mutually exclusive. Please specify only one.");

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
        builder.Services.AddSingleton<ITaskManagerService, TaskManagerService>();
        builder.Services.AddSingleton<ISecureAttentionSequenceService, SecureAttentionSequenceService>();
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

                            Log.Information("Incoming request from IP: {Ip}", remoteIp);

                            if (remoteIp == null)
                            {
                                return Task.CompletedTask;
                            }

                            if (!remoteIp.Equals(IPAddress.Loopback) && !remoteIp.Equals(IPAddress.IPv6Loopback) && !remoteIp.Equals(localIPv6Mapped))
                            {
                                return Task.CompletedTask;
                            }

                            Log.Information("Localhost detected");

                            var identity = new ClaimsIdentity(new[]
                            {
                                new Claim(ClaimTypes.Name, "localhost@localdomain"),
                            }, "LocalAuth");

                            context.Principal = new ClaimsPrincipal(identity);
                            context.Success();

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
            builder.Services.AddHostedService<HostInformationMonitorService>();
            builder.Services.AddHostedService<HostRegistrationMonitorService>();
        }

        var app = builder.Build();

        if (install)
        {
            var hostServiceManager = app.Services.GetRequiredService<IHostServiceManager>();

            await hostServiceManager.Install();

            return;
        }

        if (uninstall)
        {
            var hostServiceManager = app.Services.GetRequiredService<IHostServiceManager>();

            await hostServiceManager.Uninstall();

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

        await app.RunAsync();
    }
}