// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.AuthorizationHandlers;
using RemoteMaster.Host.Core.HttpClientHandlers;
using RemoteMaster.Host.Core.LaunchModes;
using RemoteMaster.Host.Core.LogEnrichers;
using RemoteMaster.Host.Core.ParameterHandlers;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    private static void AddCoreParameterHandlers(this IServiceCollection services)
    {
        services.AddTransient<IParameterHandler, StringParameterHandler>();
        services.AddTransient<IParameterHandler, BooleanParameterHandler>();
    }

    private static void AddCommonCoreServices(this IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddTransient<HostInfoEnricher>();
        services.AddSingleton<IHelpService, HelpService>();
        services.AddSingleton<IHostConfigurationService, HostConfigurationService>();
        services.AddSingleton<ICertificateLoaderService, CertificateLoaderService>();
    }

    public static void AddMinimalCoreServices(this IServiceCollection services)
    {
        services.AddCommonCoreServices();
        services.AddCoreParameterHandlers();

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IArgumentParser, ArgumentParser>();
        services.AddSingleton<ILaunchModeProvider, LaunchModeProvider>();
        services.AddSingleton<IHostInformationService, HostInformationService>();
    }

    public static async Task AddCoreServices(this IServiceCollection services, LaunchModeBase launchModeInstance)
    {
        services.AddCommonCoreServices();
        services.AddSharedServices();

        services.AddTransient<CustomHttpClientHandler>();
        services.AddSingleton<IAuthorizationHandler, LocalhostOrAuthenticatedHandler>();
        services.AddSingleton<IHostInformationUpdaterService, HostInformationUpdaterService>();
        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<ICertificateRequestService, CertificateRequestService>();
        services.AddSingleton<IHostLifecycleService, HostLifecycleService>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        services.AddTransient<IViewerFactory, ViewerFactory>();
        services.AddSingleton<IScreenCastingService, ScreenCastingService>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();

        services.AddHttpClient<ApiService>().AddHttpMessageHandler<CustomHttpClientHandler>();

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

                var validateLifetime = !IsWinPE();

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(jwtBearerOptions =>
                    {
                        jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateIssuerSigningKey = true,
                            ValidateLifetime = validateLifetime,
                            ValidIssuer = "RemoteMaster Server",
                            ValidAudience = "RMServiceAPI",
                            IssuerSigningKey = new RsaSecurityKey(rsa),
                            RoleClaimType = ClaimTypes.Role,
                            AuthenticationType = "JWT Security"
                        };
                    });

                services.AddAuthorizationBuilder().AddCustomPolicies();
            }
        }

        services.AddSignalR().AddMessagePackProtocol(options => options.Configure());

        switch (launchModeInstance)
        {
            case UserMode:
                services.AddHostedService<InputBackgroundService>();
                break;
            case ServiceMode:
                services.AddHostedService<CertificateManagementService>();
                services.AddHostedService<HostProcessMonitorService>();
                services.AddHostedService<HostRegistrationMonitorService>();
                break;
        }
    }

    private static bool IsWinPE()
    {
        var systemDirectory = Environment.SystemDirectory;

        var systemDrive = Path.GetPathRoot(systemDirectory);

        return !string.Equals(systemDrive, @"C:\", StringComparison.OrdinalIgnoreCase);
    }
}
