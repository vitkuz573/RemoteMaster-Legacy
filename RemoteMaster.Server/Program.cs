// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<NodesDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContextFactory<CertificateDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
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
builder.Services.AddSingleton<IBrandingService, BrandingService>();
builder.Services.AddSingleton<ICertificateService, CertificateService>();
builder.Services.AddSingleton<IPacketSender, UdpPacketSender>();
builder.Services.AddSingleton<IWakeOnLanService, WakeOnLanService>();
builder.Services.AddSingleton<IComputerConnectivityService, ComputerConnectivityService>();
builder.Services.AddSingleton<ICaCertificateService, CaCertificateService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IJwtSecurityService, JwtSecurityService>();

builder.Services.AddSingleton(new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SubjectOptions>(builder.Configuration.GetSection("CASettings:Subject"));
builder.Services.Configure<CertificateOptions>(builder.Configuration.GetSection("CASettings"));
builder.Services.Configure<ApplicationSettings>(builder.Configuration);

builder.Services.AddMudServices();

builder.Host.UseSerilog((_, configuration) =>
{
    configuration.MinimumLevel.Error();
    configuration.WriteTo.Console();
    configuration.WriteTo.File(@"C:\ProgramData\RemoteMaster\Server\RemoteMaster_Server.log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddControllers();

builder.Services.AddSignalR().AddMessagePackProtocol();

var app = builder.Build();

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

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5254");

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

var isRegisterAllowed = builder.Configuration.GetValue<bool>("RegisterAllowed");

app.UseMiddleware<RegistrationRestrictionMiddleware>(isRegisterAllowed);
app.UseMiddleware<RouteRestrictionMiddleware>();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapHub<ManagementHub>("/hubs/management");

var caCertificateService = app.Services.GetRequiredService<ICaCertificateService>();
caCertificateService.EnsureCaCertificateExists();

var jwtSecurityService = app.Services.GetRequiredService<IJwtSecurityService>();
jwtSecurityService.EnsureKeysExist();

app.Run();
