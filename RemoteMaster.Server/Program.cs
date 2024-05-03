// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;
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
            configuration.MinimumLevel.Information()
                         .WriteTo.Console()
                         .WriteTo.File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "Server", "RemoteMaster_Server.log"), rollingInterval: RollingInterval.Day)
                         .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                         .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
        });

        builder.WebHost.UseWebRoot("wwwroot");

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
        services.AddScoped<IComputerConnectivityService, ComputerConnectivityService>();
        services.AddSingleton<IBrandingService, BrandingService>();
        services.AddSingleton<ICertificateService, CertificateService>();
        services.AddSingleton<IPacketSender, UdpPacketSender>();
        services.AddSingleton<IWakeOnLanService, WakeOnLanService>();
        services.AddSingleton<ICaCertificateService, CaCertificateService>();
        services.AddSingleton<IJwtSecurityService, JwtSecurityService>();

        services.AddSingleton(new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        services.AddTransient<ITokenService, TokenService>();

        services.AddHostedService<SecurityInitializationService>();
        services.AddHostedService<RoleInitializationService>();
        services.AddHostedService<DatabaseCleanerService>();

        services.Configure<ApplicationSettings>(configurationManager);
        services.Configure<JwtOptions>(configurationManager.GetSection("jwt"));
        services.Configure<CertificateOptions>(configurationManager.GetSection("caSettings"));
        services.Configure<SubjectOptions>(configurationManager.GetSection("caSettings:subject"));

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

                var response = new
                {
                    status = report.Status.ToString(),
                    overallStatusCode = report.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
                    timestamp = DateTime.UtcNow,
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        statusCode = entry.Value.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
                        duration = entry.Value.Duration.ToString(),
                        description = GetDescriptionByCheckName(entry.Key),
                        exception = entry.Value.Exception?.Message,
                        data = entry.Value.Data.Select(kv => new { key = kv.Key, value = kv.Value.ToString() })
                    })
                };

                var result = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                await context.Response.WriteAsync(result);
            }
        });

        static string GetDescriptionByCheckName(string checkName)
        {
            return checkName switch
            {
                "SqlServer" => "Checks if the SQL Server database is reachable and responsive within the given timeout.",
                "ApplicationDbContext" => "Verifies that the ApplicationDbContext can connect to its database and run a sample query within the given timeout.",
                "CertificateDbContext" => "Checks connectivity and query ability of the CertificateDbContext against its database within a set timeout.",
                "NodesDbContext" => "Ensures that the NodesDbContext can communicate with its designated database and execute a basic query within the expected time frame.",
                _ => "No description available."
            };
        }

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

        app.Use(async (context, next) =>
        {
            var connectionInfo = context.Connection;

            if (connectionInfo.LocalPort == 5254 && !context.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Response.StatusCode = 404;

                return;
            }

            await next();
        });

        app.UseMiddleware<RegistrationRestrictionMiddleware>();
        app.UseMiddleware<RouteRestrictionMiddleware>();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapControllers();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();

        app.MapHub<ManagementHub>("/hubs/management").RequireHost("*:5254");
    }
}