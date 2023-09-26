// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Radzen;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Areas.Identity.Data;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Middlewares;
using RemoteMaster.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Setup services
ConfigureServices(builder);

// Setup application
var app = ConfigureApplication(builder);

app.Run();

void ConfigureServices(WebApplicationBuilder builder)
{
    // Core services
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.AccessDeniedPath = "/Identity/Account/Login";
        options.LoginPath = "/Identity/Account/Login";
    });

    builder.Services.AddHttpClient("DefaultClient", client =>
    {
        client.BaseAddress = new Uri("http://127.0.0.1:5254");
    });

    // Business services
    builder.Services.AddTransient<IConfiguratorService, ConfiguratorService>();
    builder.Services.AddScoped<IConnectionManager, ConnectionManager>();
    builder.Services.AddTransient<IConnectionContextFactory, ConnectionContextFactory>();
    builder.Services.AddScoped<DatabaseService>();
    builder.Services.AddSingleton<IPacketSender, UdpPacketSender>();
    builder.Services.AddSingleton<IWakeOnLanService, WakeOnLanService>();

    // Hub services
    builder.Services.AddTransient<IHubConnectionBuilder>(s => new HubConnectionBuilder());

    // Database contexts
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddDbContext<IdentityDataContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<IdentityDataContext>();

    // UI services
    builder.Services.AddScoped<DialogService>();
    builder.Services.AddScoped<NotificationService>();
    builder.Services.AddScoped<TooltipService>();
    builder.Services.AddScoped<ContextMenuService>();

    // Blazor services
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
}

WebApplication ConfigureApplication(WebApplicationBuilder builder)
{
    var app = builder.Build();

    // Perform database migrations
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();

        var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDataContext>();
        identityContext.Database.Migrate();
    }

    app.Urls.Clear();
    app.Urls.Add("http://0.0.0.0:5254");

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    var enableRegistration = builder.Configuration.GetValue<bool>("EnableRegistration");

    app.UseMiddleware<RegistrationRestrictionMiddleware>(enableRegistration);
    app.UseMiddleware<RouteRestrictionMiddleware>();

    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // Routes
    app.MapControllers();
    app.MapBlazorHub();
    app.MapHub<ManagementHub>("/hubs/management");
    app.MapFallbackToPage("/_Host");

    return app;
}
