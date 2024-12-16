// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Net.Sockets;
using System.Net.WebSockets;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.AuthorizationHandlers;
using RemoteMaster.Server.Components;
using RemoteMaster.Server.Components.Account;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.DomainEvents;
using RemoteMaster.Server.Extensions;
using RemoteMaster.Server.Middlewares;
using RemoteMaster.Server.Options;
using RemoteMaster.Server.Repositories;
using RemoteMaster.Server.Services;
using RemoteMaster.Server.UnitOfWork;
using RemoteMaster.Server.Validators;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Converters;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Services;
using Serilog;

namespace RemoteMaster.Server;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog(logger);

        ConfigureServices(builder.Services, builder.Configuration);

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenAnyIP(80);
            serverOptions.ListenAnyIP(5254);
        });

        var app = builder.Build();

        ConfigurePipeline(app);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, ConfigurationManager configurationManager)
    {
        services.AddResilienceEnricher();

        services.AddResiliencePipeline<string, string>("Resilience-Pipeline", builder =>
        {
            builder.AddRetry(new RetryStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<WebSocketException>()
                    .Handle<IOException>()
                    .Handle<SocketException>()
                    .Handle<InvalidOperationException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Exponential
            });

            builder.AddFallback(new FallbackStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<HubException>(ex => ex.Message.Contains("Method does not exist")),
                FallbackAction = async _ =>
                {
                    await Task.CompletedTask;

                    return Outcome.FromResult("This function is not available in the current host version. Please update your host.");
                }
            });
        });

        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        services.AddScoped<IAuthorizationHandler, HostAccessHandler>();

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddDbContext<ApplicationDbContext>();
        services.AddDbContext<CrlDbContext>();
        services.AddDbContext<CertificateTaskDbContext>();

        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddHttpClient();

        services.AddSharedServices();

        services.AddCertificateAuthorityService();

        services.AddTransient<IUdpClient, UdpClientWrapper>();
        services.AddTransient<Func<IUdpClient>>(provider => provider.GetRequiredService<IUdpClient>);
        services.AddScoped<IDomainEventHandler<OrganizationAddressChangedEvent>, OrganizationAddressChangedEventHandler>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IApplicationClaimRepository, ApplicationClaimRepository>();
        services.AddScoped<ICrlRepository, CrlRepository>();
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ICertificateRenewalTaskRepository, CertificateRenewalTaskRepository>();
        services.AddScoped<IHostCommandService, HostCommandService>();
        services.AddScoped<ICrlService, CrlService>();
        services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();
        services.AddScoped<IClaimsService, ClaimsService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICertificateProvider, CertificateProvider>();
        services.AddScoped<IHostRegistrationService, HostRegistrationService>();
        services.AddScoped<IUserPlanProvider, UserPlanProvider>();
        services.AddScoped<ILimitChecker, LimitChecker>();
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationalUnitService, OrganizationalUnitService>();
        services.AddScoped<IEventNotificationService, TelegramEventNotificationService>();
        services.AddScoped<IHostMoveRequestService, HostMoveRequestService>();
        services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();
        services.AddScoped<ICrlUnitOfWork, CrlUnitOfWork>();
        services.AddScoped<ICertificateTaskUnitOfWork, CertificateTaskUnitOfWork>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IHostAccessService, HostAccessService>();
        services.AddSingleton<ITokenStorageService, CookieTokenStorageService>();
        services.AddSingleton<IBrandingService, BrandingService>();
        services.AddSingleton<ICertificateService, CertificateService>();
        services.AddSingleton<IPacketSender, UdpPacketSender>();
        services.AddSingleton<IWakeOnLanService, WakeOnLanService>();
        services.AddSingleton<IJwtSecurityService, JwtSecurityService>();
        services.AddSingleton<IRemoteExecutionService, RemoteExecutionService>();
        services.AddSingleton<INetworkDriveService, NetworkDriveService>();
        services.AddSingleton<ICountryProvider, CountryProvider>();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IValidateOptions<CertificateAuthorityOptions>, CertificateAuthorityOptionsValidator>();
        services.AddSingleton<IValidateOptions<SubjectOptions>, SubjectOptionsValidator>();
        services.AddSingleton<IValidateOptions<TelegramBotOptions>, TelegramBotOptionsValidator>();
        services.AddSingleton<INotificationService, InMemoryNotificationService>();
        services.AddSingleton<IPlanService, PlanService>();
        services.AddSingleton<ITokenSigningService, RsaTokenSigningService>();
        services.AddSingleton<ITokenValidationService, RsaTokenValidationService>();
        services.AddSingleton<ISslWarningService, SslWarningService>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IHostInformationService, HostInformationService>();
        services.AddSingleton<ISubjectService, SubjectService>();
        services.AddSingleton<ICertificateStoreService, CertificateStoreService>();

        services.AddHostedService<MigrationService>();
        services.AddHostedService<RoleInitializationService>();
        services.AddHostedService<SecurityInitializationService>();
        services.AddHostedService<DatabaseCleanerService>();
        services.AddHostedService<CertificateRenewalTaskService>();

        services.Configure<UpdateOptions>(configurationManager.GetSection("update"));
        services.Configure<TelegramBotOptions>(configurationManager.GetSection("telegramBot"));
        services.Configure<JwtOptions>(configurationManager.GetSection("jwt"));
        services.Configure<CertificateAuthorityOptions>(configurationManager.GetSection("certificateAuthority"));
        services.Configure<InternalCertificateOptions>(configurationManager.GetSection("certificateAuthority:internalOptions"));
        services.Configure<ActiveDirectoryOptions>(configurationManager.GetSection("certificateAuthority:activeDirectoryOptions"));
        services.Configure<SubjectOptions>(configurationManager.GetSection("certificateAuthority:internalOptions:subject"));
        services.Configure<WimBootOptions>(configurationManager.GetSection("wimBoot"));

        services.AddMudServices();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new IPAddressConverter());
                options.JsonSerializerOptions.Converters.Add(new PhysicalAddressConverter());
            });

        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = new MediaTypeApiVersionReader("v");
        }).AddMvc().AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
        });

        services.AddEndpointsApiExplorer();

        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configurationManager.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
                name: "SqlServer",
                failureStatus: HealthStatus.Degraded,
                tags: ["db", "sql"],
                timeout: TimeSpan.FromSeconds(5)
            )
            .AddDbContextCheck<ApplicationDbContext>(
                name: "ApplicationDbContext",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "entityframework"]
            )
            .AddDbContextCheck<CrlDbContext>(
                name: "CrlDbContext",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "entityframework"]
            );

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("ManagementHubPolicy", opts =>
            {
                opts.PermitLimit = 10;
                opts.Window = TimeSpan.FromMinutes(1);
            });

            options.AddFixedWindowLimiter("AuthRefreshPolicy", opts =>
            {
                opts.PermitLimit = 5;
                opts.Window = TimeSpan.FromMinutes(10);
            });

            options.AddSlidingWindowLimiter("CrlPolicy", opts =>
            {
                opts.PermitLimit = 3;
                opts.Window = TimeSpan.FromMinutes(5);
                opts.SegmentsPerWindow = 5;
            });

            options.AddTokenBucketLimiter("HostDownloadPolicy", opts =>
            {
                opts.TokenLimit = 10;
                opts.QueueLimit = 2;
                opts.ReplenishmentPeriod = TimeSpan.FromHours(1);
                opts.TokensPerPeriod = 10;
            });

            options.OnRejected = (ctx, _) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                return ValueTask.CompletedTask;
            };
        });
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseMiddleware<RegistrationRestrictionMiddleware>();
        app.UseMiddleware<PortRestrictionMiddleware>(5254);

        app.UseAntiforgery();

        app.MapControllers()
            .RequireHost($"*:{5254}");

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();
    }
}
