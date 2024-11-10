// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Host.Core;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.AuthorizationHandlers;
using RemoteMaster.Host.Core.Exceptions;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Core.LaunchModes;
using RemoteMaster.Host.Core.Requirements;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Hubs;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Host.Windows.ScreenOverlays;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var minimalServices = new ServiceCollection();
        ConfigureMinimalServices(minimalServices);
        var minimalServiceProvider = minimalServices.BuildServiceProvider();

        try
        {
            var argumentParser = minimalServiceProvider.GetRequiredService<IArgumentParser>();
            var launchModeInstance = argumentParser.ParseArguments(args);

            if (launchModeInstance == null)
            {
                Environment.Exit(1);
            }

            var options = new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory
            };

            var builder = WebApplication.CreateSlimBuilder(options);
            builder.Configuration.AddCommandLine(args);

            builder.Host.UseWindowsService();

            await ConfigureServices(builder.Services, launchModeInstance);

            var serviceProvider = builder.Services.BuildServiceProvider();
            var hostConfigurationService = serviceProvider.GetRequiredService<IHostConfigurationService>();
            var hostInfoEnricher = serviceProvider.GetRequiredService<HostInfoEnricher>();
            var certificateLoaderService = serviceProvider.GetRequiredService<ICertificateLoaderService>();

            await builder.ConfigureSerilog(launchModeInstance, hostConfigurationService, hostInfoEnricher);
            builder.ConfigureCoreUrls(launchModeInstance, certificateLoaderService);

            builder.Services.AddCors(builder => builder.AddDefaultPolicy(opts =>
                opts.AllowCredentials().SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod()));

            var app = builder.Build();

            app.UseCors();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            if (launchModeInstance is not InstallMode)
            {
                app.UseAuthentication();
                app.UseAuthorization();
            }

            app.MapCoreHubs(launchModeInstance);

            if (launchModeInstance is UserMode)
            {
                app.MapHub<ServiceHub>("/hubs/service");
                app.MapHub<DeviceManagerHub>("/hubs/devicemanager");
                app.MapHub<RegistryHub>("/hubs/registry");
            }

            app.Lifetime.ApplicationStarted.Register(Callback);

            await app.RunAsync();

            async void Callback()
            {
                await launchModeInstance.ExecuteAsync(app.Services);
            }
        }
        catch (MissingParametersException ex)
        {
            var helpService = minimalServiceProvider.GetRequiredService<IHelpService>();

            helpService.PrintMissingParametersError(ex.LaunchModeName, ex.MissingParameters);

            Environment.Exit(1);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");

            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");

            Environment.Exit(1);
        }
    }

    private static void ConfigureMinimalServices(IServiceCollection services)
    {
        services.AddMinimalCoreServices();
    }

    private static async Task ConfigureServices(IServiceCollection services, LaunchModeBase launchModeInstance)
    {
        services.AddHttpContextAccessor();

        services.AddCoreServices();
        services.AddTransient<IServiceFactory, ServiceFactory>();
        services.AddTransient<IRegistryKeyFactory, RegistryKeyFactory>();
        services.AddTransient<IProcessWrapperFactory, ProcessWrapperFactory>();
        services.AddTransient<INativeProcessFactory, NativeProcessFactory>();
        services.AddSingleton<AbstractService, HostService>();
        services.AddSingleton<IUserInstanceService, UserInstanceService>();
        services.AddSingleton<IUpdaterInstanceService, UpdaterInstanceService>();
        services.AddSingleton<IChatInstanceService, ChatInstanceService>();
        services.AddSingleton<IHostInstaller, HostInstaller>();
        services.AddSingleton<IHostUninstaller, HostUninstaller>();
        services.AddSingleton<IScreenCapturingService, GdiCapturing>();
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
        services.AddSingleton<IInstanceManagerService, InstanceManagerService>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IWorkStationSecurityService, WorkStationSecurityService>();
        services.AddSingleton<IAuthorizationHandler, LocalhostOrAuthenticatedHandler>();
        services.AddSingleton<ICommandExecutor, CommandExecutor>();
        services.AddSingleton<IDeviceManagerService, DeviceManagerService>();
        services.AddSingleton<IOperatingSystemInformationService, OperatingSystemInformationService>();
        services.AddSingleton<ITrayIconManager, TrayIconManager>();
        services.AddSingleton<ClickIndicatorOverlay>();
        services.AddSingleton<IScreenOverlay>(provider => provider.GetRequiredService<ClickIndicatorOverlay>());

        if (launchModeInstance is not InstallMode)
        {
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");

            if (File.Exists(publicKeyPath))
            {
                var publicKey = await File.ReadAllBytesAsync(publicKeyPath);

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
                    .AddPolicy("ChangeSelectedScreenPolicy", policy =>
                        policy.RequireClaim("Screen", "ChangeSelectedScreen"))
                    .AddPolicy("SetFrameRatePolicy", policy =>
                        policy.RequireClaim("Screen", "SetFrameRate"))
                    .AddPolicy("SetImageQualityPolicy", policy =>
                        policy.RequireClaim("Screen", "SetImageQuality"))
                    .AddPolicy("ToggleDrawCursorPolicy", policy =>
                        policy.RequireClaim("Screen", "ToggleDrawCursor"))
                    .AddPolicy("SetCodecPolicy", policy =>
                        policy.RequireClaim("Screen", "SetCodec"))
                    .AddPolicy("MouseInputPolicy", policy =>
                        policy.RequireClaim("Input", "MouseInput"))
                    .AddPolicy("KeyboardInputPolicy", policy =>
                        policy.RequireClaim("Input", "KeyboardInput"))
                    .AddPolicy("ToggleInputPolicy", policy =>
                        policy.RequireClaim("Input", "ToggleInput"))
                    .AddPolicy("ToggleClickIndicator", policy =>
                        policy.RequireClaim("Input", "ToggleClickIndicator"))
                    .AddPolicy("BlockUserInputPolicy", policy =>
                        policy.RequireClaim("Input", "BlockUserInput"))
                    .AddPolicy("RebootHostPolicy", policy =>
                        policy.RequireClaim("Power", "RebootHost"))
                    .AddPolicy("ShutdownHostPolicy", policy =>
                        policy.RequireClaim("Power", "ShutdownHost"))
                    .AddPolicy("SetMonitorStatePolicy", policy =>
                        policy.RequireClaim("Hardware", "SetMonitorState"))
                    .AddPolicy("ExecuteScriptPolicy", policy =>
                        policy.RequireClaim("Execution", "Scripts"))
                    .AddPolicy("LockWorkStationPolicy", policy =>
                        policy.RequireClaim("Security", "LockWorkStation"))
                    .AddPolicy("LogOffUserPolicy", policy =>
                        policy.RequireClaim("Security", "LogOffUser"))
                    .AddPolicy("TerminateHostPolicy", policy =>
                        policy.RequireClaim("HostManagement", "TerminateHost"))
                    .AddPolicy("MoveHostPolicy", policy =>
                        policy.RequireClaim("HostManagement", "Move"))
                    .AddPolicy("RenewCertificatePolicy", policy =>
                        policy.RequireClaim("HostManagement", "RenewCertificate"))
                    .AddPolicy("DisconnectClientPolicy", policy =>
                        policy.RequireClaim("Service", "DisconnectClient"));
            }
        }

        switch (launchModeInstance)
        {
            case UserMode:
                services.AddHostedService<WoLInitializationService>();
                services.AddHostedService<FirewallInitializationService>();
                services.AddHostedService<SasInitializationService>();
                services.AddHostedService<InputBackgroundService>();
                services.AddHostedService<TrayIconHostedService>();
                break;
            case ServiceMode:
                services.AddHostedService<CommandListenerService>();
                services.AddHostedService<CertificateManagementService>();
                services.AddHostedService<HostProcessMonitorService>();
                services.AddHostedService<HostRegistrationMonitorService>();
                services.AddHostedService<MessageLoopService>();
                break;
            case ChatMode:
                services.AddHostedService<ChatWindowService>();
                break;
        }
    }
}
