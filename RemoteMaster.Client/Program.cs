// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Radzen;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IHubConnectionFactory, HubConnectionFactory>();

// Services
builder.Services.AddScoped<ControlFunctionsService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddSingleton<ActiveDirectoryService>();
builder.Services.AddTransient<IHubConnectionBuilder>(s => new HubConnectionBuilder());
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
