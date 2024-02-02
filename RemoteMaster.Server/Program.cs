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
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(builder =>
{
    builder.ClearProviders();
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
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<NodesDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddTransient<IHostConfigurationService, HostConfigurationService>();
builder.Services.AddScoped<IQueryParameterService, QueryParameterService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IComputerCommandService, ComputerCommandService>();
builder.Services.AddSingleton<IBrandingService, BrandingService>();
builder.Services.AddSingleton<ICertificateService, CertificateService>();
builder.Services.AddSingleton<IPacketSender, UdpPacketSender>();
builder.Services.AddSingleton<IWakeOnLanService, WakeOnLanService>();

builder.Services.AddSingleton(new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<CertificateOptions>(builder.Configuration.GetSection("CertificateSettings"));
builder.Services.Configure<ApplicationSettings>(builder.Configuration);

builder.Services.AddMudServices();

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.MinimumLevel.Debug();
    configuration.WriteTo.Console();
    configuration.WriteTo.File(@"C:\ProgramData\RemoteMaster\Server\RemoteMaster_Server.log", rollingInterval: RollingInterval.Day);
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Initializing component"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Rendering component"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Disposing component"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("JS interop"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Received hub invocation"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("DbCommand"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("DbConnection"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Received confirmation"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Handling event"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Invoking instance method"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Completing batch"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Circuit"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("entry for"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("connection to database"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Sending render batch"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Update root components"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("started tracking"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("Invocation of"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("data reader"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("execution expression"));
    configuration.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.Contains("endpoint"));
});

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5254");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();

var isRegisterAllowed = builder.Configuration.GetValue<bool>("RegisterAllowed");

app.UseMiddleware<RegistrationRestrictionMiddleware>(isRegisterAllowed);
app.UseMiddleware<RouteRestrictionMiddleware>();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapHub<ManagementHub>("/hubs/management");

app.Run();
