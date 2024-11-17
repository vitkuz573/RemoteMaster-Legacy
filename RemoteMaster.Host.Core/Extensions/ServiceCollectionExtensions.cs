// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using System.Security.Cryptography;
using MessagePack;
using MessagePack.Resolvers;
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
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Formatters;

namespace RemoteMaster.Host.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCoreParameterHandlers(this IServiceCollection services)
    {
        services.AddTransient<IParameterHandler, StringParameterHandler>();
        services.AddTransient<IParameterHandler, BooleanParameterHandler>();
    }

    public static void AddMinimalCoreServices(this IServiceCollection services)
    {
        services.AddCoreParameterHandlers();

        services.AddSingleton<IHelpService, HelpService>();
        services.AddSingleton<IArgumentParser, ArgumentParser>();
        services.AddSingleton<ILaunchModeProvider, LaunchModeProvider>();
    }

    public static async Task AddCoreServices(this IServiceCollection services, LaunchModeBase launchModeInstance)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddSharedServices();

        services.AddTransient<HostInfoEnricher>();
        services.AddTransient<CustomHttpClientHandler>();
        services.AddSingleton<IAuthorizationHandler, LocalhostOrAuthenticatedHandler>();
        services.AddSingleton<IHelpService, HelpService>();
        services.AddSingleton<IHostInformationUpdaterService, HostInformationUpdaterService>();
        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<ICertificateRequestService, CertificateRequestService>();
        services.AddSingleton<IHostLifecycleService, HostLifecycleService>();
        services.AddSingleton<IHostConfigurationService, HostConfigurationService>();
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<IShutdownService, ShutdownService>();
        services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
        services.AddTransient<IViewerFactory, ViewerFactory>();
        services.AddSingleton<ICertificateLoaderService, CertificateLoaderService>();
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

        services.AddSignalR().AddMessagePackProtocol(options =>
        {
            var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

            options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        });

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
