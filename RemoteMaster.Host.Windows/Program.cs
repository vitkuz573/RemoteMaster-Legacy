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
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows;

internal class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="launchMode">Runs the program in specified mode.</param>
    /// <returns></returns>
    private static async Task Main(LaunchMode launchMode = LaunchMode.Default, string folderPath = "", string? username = null, string? password = null)
    {
        var options = new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
        };

        var builder = WebApplication.CreateSlimBuilder(options);
        builder.Host.UseWindowsService();

        builder.Services.AddCoreServices(launchMode is not LaunchMode.Updater);
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

        switch (launchMode)
        {
            case LaunchMode.User:
                builder.ConfigureCoreUrls();
                builder.Services.AddHostedService<HostRegistrationMonitorService>();
                break;
            case LaunchMode.Service:
                builder.Services.AddHostedService<MessageLoopService>();
                builder.Services.AddHostedService<HostProcessMonitorService>();
                builder.Services.AddHostedService<CommandListenerService>();
                builder.Services.AddHostedService<HostInformationMonitorService>();
                break;
        }

        var app = builder.Build();

        switch (launchMode)
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

        if (launchMode == LaunchMode.User)
        {
            app.MapCoreHubs();
        }

        if (launchMode is LaunchMode.Updater)
        {
            var hostUpdater = app.Services.GetRequiredService<IHostUpdater>();

            await hostUpdater.UpdateAsync(folderPath, username, password);
        }
        else
        {
            await app.RunAsync();
        }
    }
}