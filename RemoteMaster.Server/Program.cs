// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Areas.Identity.Data;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Middlewares;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder);
var app = ConfigureApplication(builder);

app.Run();

void ConfigureServices(WebApplicationBuilder builder)
{
    ConfigureCoreServices(builder);
    ConfigureBusinessServices(builder);
    ConfigureDatabaseContexts(builder);
    ConfigureUIServices(builder);
}

void ConfigureCoreServices(WebApplicationBuilder builder)
{
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.AccessDeniedPath = "/Identity/Account/Login";
        options.LoginPath = "/Identity/Account/Login";
    });

    builder.Services.AddHttpClient();
}

void ConfigureBusinessServices(WebApplicationBuilder builder)
{
    builder.Services.AddTransient<IHostConfigurationService, HostConfigurationService>();
    builder.Services.AddScoped<DatabaseService>();
    builder.Services.AddSingleton<ICertificateService, CertificateService>();
    builder.Services.AddSingleton<IPacketSender, UdpPacketSender>();
    builder.Services.AddSingleton<IWakeOnLanService, WakeOnLanService>();
    builder.Services.AddSingleton<ISerializationService, JsonSerializerService>();
    builder.Services.AddTransient<ITokenService, TokenService>();
    builder.Services.Configure<TokenServiceOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<CertificateSettings>(builder.Configuration.GetSection("CertificateSettings"));
}

void ConfigureDatabaseContexts(WebApplicationBuilder builder)
{
    builder.Services.AddDbContext<NodesDataContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("NodesDataContextConnection")));

    builder.Services.AddDbContext<IdentityDataContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("IdentityDataContextConnection")));

    builder.Services.AddDefaultIdentity<IdentityUser>()
        .AddEntityFrameworkStores<IdentityDataContext>();
}

void ConfigureUIServices(WebApplicationBuilder builder)
{
    builder.Services.AddMudServices();
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
}

WebApplication ConfigureApplication(WebApplicationBuilder builder)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5254);
    });

    var app = builder.Build();

    PerformDatabaseMigrations(app);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    var isRegisterAllowed = builder.Configuration.GetValue<bool>("RegisterAllowed");

    app.UseMiddleware<RegistrationRestrictionMiddleware>(isRegisterAllowed);
    app.UseMiddleware<RouteRestrictionMiddleware>();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    ConfigureRoutes(app);

    return app;
}

void PerformDatabaseMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<NodesDataContext>();
    dbContext.Database.Migrate();

    var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDataContext>();
    identityContext.Database.Migrate();
}

void ConfigureRoutes(WebApplication app)
{
    app.MapControllers();
    app.MapBlazorHub();
    app.MapHub<ManagementHub>("/hubs/management");
    app.MapFallbackToPage("/_Host");
}
