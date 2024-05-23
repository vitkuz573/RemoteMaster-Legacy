// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Components;
using RemoteMaster.Server.Components.Account;
using RemoteMaster.Server.Components.Library.Extensions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Middlewares;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Models;
using Serilog;
using Serilog.Events;

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
                { "Microsoft.AspNetCore", LogEventLevel.Warning }
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
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
        });

        services.AddRazorComponents().AddInteractiveServerComponents();

        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        }).AddIdentityCookies();

        var connectionString = configurationManager.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Could not find a connection string named 'DefaultConnection'.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddDbContext<NodesDbContext>(options => options.UseSqlServer(connectionString));
        services.AddDbContextFactory<CertificateDbContext>(options => options.UseSqlServer(connectionString));

        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddHttpClient();

        services.AddSharedServices();

        services.AddScoped<IQueryParameterService, QueryParameterService>();
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddScoped<IComputerCommandService, ComputerCommandService>();
        services.AddScoped<ICrlService, CrlService>();
        services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IBrandingService, BrandingService>();
        services.AddSingleton<ICertificateService, CertificateService>();
        services.AddSingleton<IPacketSender, UdpPacketSender>();
        services.AddSingleton<IWakeOnLanService, WakeOnLanService>();
        services.AddSingleton<ICaCertificateService, CaCertificateService>();
        services.AddSingleton<IJwtSecurityService, JwtSecurityService>();
        services.AddSingleton<ITokenStorageService, InMemoryTokenStorageService>();

        services.AddLibraryServices();

        services.AddSingleton(new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        services.AddHostedService<SecurityInitializationService>();
        services.AddHostedService<RoleInitializationService>();
        services.AddHostedService<DatabaseCleanerService>();

        services.Configure<ApplicationSettings>(configurationManager);
        services.Configure<JwtOptions>(configurationManager.GetSection("jwt"));
        services.Configure<CertificateOptions>(configurationManager.GetSection("caSettings"));
        services.Configure<SubjectOptions>(configurationManager.GetSection("caSettings:subject"));

        services.AddCookiePolicy(options =>
        {
            options.HttpOnly = HttpOnlyPolicy.Always;
            options.MinimumSameSitePolicy = SameSiteMode.Strict;
            options.Secure = CookieSecurePolicy.SameAsRequest;

            options.OnAppendCookie = cookieContext =>
            {
                if (cookieContext.CookieName == CookieNames.AccessToken)
                {
                    cookieContext.CookieOptions.Expires = DateTime.UtcNow.AddMinutes(15);
                }
                else if (cookieContext.CookieName == CookieNames.RefreshToken)
                {
                    cookieContext.CookieOptions.Expires = DateTime.UtcNow.AddDays(1);
                }
            };
        });

        services.AddMudServices();

        services.AddMemoryCache();

        services.AddControllers();

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
            .AddDbContextCheck<NodesDbContext>(
                name: "NodesDbContext",
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

            options.OnRejected = (ctx, token) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                return ValueTask.CompletedTask;
            };
        });
    }

    private static void ConfigurePipeline(WebApplication app)
    {
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

                var responseModel = ApiResponse<List<HealthCheck>>.Success(healthCheckResults, "Health checks completed");
                var jsonResponse = JsonSerializer.Serialize(responseModel, new JsonSerializerOptions { WriteIndented = true });

                await context.Response.WriteAsync(jsonResponse);
            }
        });

        app.UseRateLimiter();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                var dbContexts = new List<DbContext>
                {
                    services.GetRequiredService<ApplicationDbContext>(),
                    services.GetRequiredService<NodesDbContext>(),
                    services.GetRequiredService<CertificateDbContext>(),
                };

                foreach (var dbContext in dbContexts)
                {
                    dbContext.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while applying the database migrations.");
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        var applicationSettings = app.Services.GetRequiredService<IOptions<ApplicationSettings>>().Value;

        app.UseCookiePolicy();

        app.Use(async (context, next) =>
        {
            if (context.Connection.LocalPort == 5254 && !context.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Response.StatusCode = 404;

                return;
            }

            await next();
        });

        app.UseMiddleware<RegistrationRestrictionMiddleware>();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapControllers();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();

        app.MapHub<ManagementHub>("/hubs/management").RequireHost("*:5254");
    }
}