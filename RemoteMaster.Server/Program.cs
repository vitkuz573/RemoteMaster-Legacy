// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Could not find a connection string named 'DefaultConnection'.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<NodesDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContextFactory<CertificateDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddHttpClient();

builder.Services.AddSharedServices();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<IQueryParameterService, QueryParameterService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IComputerCommandService, ComputerCommandService>();
builder.Services.AddScoped<ICrlService, CrlService>();
builder.Services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();
builder.Services.AddScoped<IComputerConnectivityService, ComputerConnectivityService>();
builder.Services.AddSingleton<IBrandingService, BrandingService>();
builder.Services.AddSingleton<ICertificateService, CertificateService>();
builder.Services.AddSingleton<IPacketSender, UdpPacketSender>();
builder.Services.AddSingleton<IWakeOnLanService, WakeOnLanService>();
builder.Services.AddSingleton<ICaCertificateService, CaCertificateService>();
builder.Services.AddSingleton<IJwtSecurityService, JwtSecurityService>();

builder.Services.AddSingleton(new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

builder.Services.AddTransient<ITokenService, TokenService>();

builder.Services.Configure<ApplicationSettings>(builder.Configuration);
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("jwt"));
builder.Services.Configure<CertificateOptions>(builder.Configuration.GetSection("caSettings"));
builder.Services.Configure<SubjectOptions>(builder.Configuration.GetSection("caSettings:subject"));

builder.Services.AddMudServices();

builder.Host.UseSerilog((_, configuration) =>
{
    configuration.MinimumLevel.Information()
                 .WriteTo.Console()
                 .WriteTo.File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "Server", "RemoteMaster_Server.log"), rollingInterval: RollingInterval.Day)
                 .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                 .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
});

builder.Services.AddControllers();

builder.Services
    .AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddDbContextCheck<ApplicationDbContext>()
    .AddDbContextCheck<CertificateDbContext>()
    .AddDbContextCheck<NodesDbContext>();

builder.Services.AddRateLimiter(options =>
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

// builder.Services.AddSignalR().AddMessagePackProtocol();

builder.WebHost.UseWebRoot("wwwroot");

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
    serverOptions.ListenAnyIP(5254);
});

var app = builder.Build();

app.MapHealthChecks("/health");

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

// app.UseHttpsRedirection();

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

app.UseMiddleware<RegistrationRestrictionMiddleware>(applicationSettings.IsRegisterAllowed);
app.UseMiddleware<RouteRestrictionMiddleware>();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapHub<ManagementHub>("/hubs/management").RequireHost("*:5254");

var caCertificateService = app.Services.GetRequiredService<ICaCertificateService>();
caCertificateService.EnsureCaCertificateExists();

var jwtSecurityService = app.Services.GetRequiredService<IJwtSecurityService>();
await jwtSecurityService.EnsureKeysExistAsync();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("RootAdministrator"))
    {
        await roleManager.CreateAsync(new IdentityRole("RootAdministrator"));
    }

    if (!await roleManager.RoleExistsAsync("Administrator"))
    {
        await roleManager.CreateAsync(new IdentityRole("Administrator"));
    }
}

app.Run();
