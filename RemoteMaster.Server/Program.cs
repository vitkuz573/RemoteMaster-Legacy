// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using System.IO.Abstractions;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
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
using RemoteMaster.Server.Components;
using RemoteMaster.Server.Components.Account;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Middlewares;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Options;
using RemoteMaster.Server.Repositories;
using RemoteMaster.Server.Requirements;
using RemoteMaster.Server.Services;
using RemoteMaster.Server.Validators;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Models;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RemoteMaster.Server;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services, builder.Configuration);

        builder.Host.UseSerilog((_, configuration) =>
        {
            var minimumLevelOverrides = new Dictionary<string, LogEventLevel>
            {
                { "Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning },
                { "Microsoft.AspNetCore", LogEventLevel.Warning },
                { "Polly", LogEventLevel.Warning }
            };

            configuration.MinimumLevel.Information()
                         .WriteTo.Console()
                         .WriteTo.File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "Server", "RemoteMaster_Server.log"), rollingInterval: RollingInterval.Day);

            foreach (var minimumLevelOverride in minimumLevelOverrides)
            {
                configuration.MinimumLevel.Override(minimumLevelOverride.Key, minimumLevelOverride.Value);
            }
        });


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
                FallbackAction = async context =>
                {
                    await Task.CompletedTask;

                    return Outcome.FromResult("This function is not available in the current host version. Please update your host.");
                }
            });
        });

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddRazorComponents().AddInteractiveServerComponents();

        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        services.AddScoped<IAuthorizationHandler, HostAccessHandler>();

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddAuthorizationBuilder()
            .AddPolicy("ToggleInputPolicy", policy => policy.RequireClaim("Input", "MouseInput"));

        var connectionString = configurationManager.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Could not find a connection string named 'DefaultConnection'.");
        }

        services.AddDbContext<TelegramBotDbContext>();
        services.AddDbContext<ApplicationDbContext>();
        services.AddDbContext<CertificateDbContext>();

        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddLocalization(options => options.ResourcesPath = "Resources");

        var defaultCulture = configurationManager.GetSection("Localization:DefaultCulture").Value ?? "en";
        var supportedCultures = new List<CultureInfo>
        {
            new("en"),
            new("ru")
        };

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(defaultCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
        });

        services.AddHttpClient();

        services.AddSharedServices();

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddTransient<IUdpClient, UdpClientWrapper>();
        services.AddTransient<Func<IUdpClient>>(provider => provider.GetRequiredService<IUdpClient>);
        services.AddScoped<ITelegramBotRepository, TelegramBotRepository>();
        services.AddScoped<IApplicationClaimRepository, ApplicationClaimRepository>();
        services.AddScoped<ICrlRepository, CrlRepository>();
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IQueryParameterService, QueryParameterService>();
        services.AddScoped<IComputerCommandService, ComputerCommandService>();
        services.AddScoped<ICrlService, CrlService>();
        services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();
        services.AddScoped<IClaimsService, ClaimsService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICertificateProvider, CertificateProvider>();
        services.AddScoped<IHostRegistrationService, HostRegistrationService>();
        services.AddScoped<IUserPlanProvider, UserPlanProvider>();
        services.AddScoped<ILimitChecker, LimitChecker>();
        services.AddScoped<ITelegramBotService, TelegramBotService>();
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationalUnitService, OrganizationalUnitService>();
        services.AddScoped<IEventNotificationService, TelegramEventNotificationService>();
        services.AddScoped<IHostMoveRequestService, HostMoveService>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<ITokenStorageService, InMemoryTokenStorageService>();
        services.AddSingleton<IBrandingService, BrandingService>();
        services.AddSingleton<ICertificateService, CertificateService>();
        services.AddSingleton<IPacketSender, UdpPacketSender>();
        services.AddSingleton<IWakeOnLanService, WakeOnLanService>();
        services.AddSingleton<ICaCertificateService, CaCertificateService>();
        services.AddSingleton<IJwtSecurityService, JwtSecurityService>();
        services.AddSingleton<IRemoteSchtasksService, RemoteSchtasksService>();
        services.AddSingleton<INetworkDriveService, NetworkDriveService>();
        services.AddSingleton<ICountryProvider, CountryProvider>();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IValidateOptions<CertificateOptions>, CertificateOptionsValidator>();
        services.AddSingleton<IValidateOptions<SubjectOptions>, SubjectOptionsValidator>();
        services.AddSingleton<IValidateOptions<TelegramBotOptions>, TelegramBotOptionsValidator>();
        services.AddSingleton<INotificationService, InMemoryNotificationService>();
        services.AddSingleton<IPlanService, PlanService>();

        services.AddSingleton(new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        services.AddHostedService<MigrationService>();
        services.AddHostedService<RoleInitializationService>();
        services.AddHostedService<SecurityInitializationService>();
        services.AddHostedService<DatabaseCleanerService>();
        services.AddHostedService<CertificateRenewalTaskService>();

        services.Configure<ApplicationSettings>(configurationManager);
        services.Configure<JwtOptions>(configurationManager.GetSection("jwt"));
        services.Configure<CertificateOptions>(configurationManager.GetSection("caSettings"));
        services.Configure<SubjectOptions>(configurationManager.GetSection("caSettings:subject"));

        services.AddMudServices();

        services.AddControllers();

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
        services.AddSwaggerGen();

        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: connectionString,
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
            .AddDbContextCheck<CertificateDbContext>(
                name: "CertificateDbContext",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "entityframework"]
            )
            .AddDbContextCheck<TelegramBotDbContext>(
                name: "TelegramBotDbContext",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "entityframework"]
            ); ;

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

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var healthCheckResults = report.Entries.Select(entry => new HealthCheck(
                    name: entry.Key,
                    status: entry.Value.Status.ToString(),
                    statusCode: entry.Value.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
                    duration: entry.Value.Duration.ToString(),
                    description: entry.Value.Description,
                    exception: entry.Value.Exception?.Message,
                    data: entry.Value.Data.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())
                )).ToList();

                var overallStatus = report.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
                var responseModel = new ApiResponse<List<HealthCheck>>(healthCheckResults, "Health checks completed", overallStatus);

                var jsonResponse = JsonSerializer.Serialize(responseModel, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await context.Response.WriteAsync(jsonResponse);
            }
        });

        app.UseRateLimiter();

        app.UseCors("AllowAll");

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (var description in provider.ApiVersionDescriptions)
                {
                    var url = $"/swagger/{description.GroupName}/swagger.json";
                    var name = description.GroupName.ToUpperInvariant();

                    options.SwaggerEndpoint($"http://localhost:5254{url}", name);
                }
            });
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseRequestLocalization();

        app.Use(async (context, next) =>
        {
            if (context.Connection.LocalPort == 5254 && !context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 404;

                return;
            }

            await next();
        });

        app.UseMiddleware<RegistrationRestrictionMiddleware>();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapControllers().RequireHost($"*:{5254}");
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();
    }
}