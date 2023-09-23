// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory
});

builder.Host.UseWindowsService();

builder.Services.AddControllers();

builder.Services.AddScoped<IServiceManager, ServiceManager>();
builder.Services.AddScoped<IComponentUpdater, ClientComponentUpdater>();
builder.Services.AddScoped<IComponentUpdater, AgentComponentUpdater>();

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5124");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapControllers();
app.UseRouting();

app.UseAuthorization();

app.Run();
