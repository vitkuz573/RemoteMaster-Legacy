// Copyright � 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Radzen;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Services;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Identity/Account/Login";
    options.LoginPath = "/Identity/Account/Login";
});

builder.Services.AddHttpClient("DefaultClient", client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:5254");
});

builder.Services.AddTransient<IConfiguratorService, ConfiguratorService>();
builder.Services.AddScoped<IConnectionManager, ConnectionManager>();
builder.Services.AddTransient<IConnectionContextFactory, ConnectionContextFactory>();
builder.Services.AddScoped<ControlFunctionsService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<IQueryParameterService, QueryParameterService>();
builder.Services.AddTransient<IHubConnectionBuilder>(s => new HubConnectionBuilder());
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<IdentityDataContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<IdentityDataContext>();

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// Blazor Pages and Server-side Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5254");

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapHub<ManagementHub>("/hubs/management");
app.MapFallbackToPage("/_Host");

app.Run();
