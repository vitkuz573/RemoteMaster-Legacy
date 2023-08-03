// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting.WindowsServices;
using RemoteMaster.Agent.Hubs;
using RemoteMaster.Shared;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

builder.Services.AddLogging(builder =>
{
    builder.AddConsole().AddDebug();
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddProvider(new FileLoggerProvider("RemoteMaster_Agent"));
});

builder.Host.UseWindowsService();

builder.Services.AddSignalR();

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:3564");

app.MapHub<MainHub>("/hubs/main");

app.Run();
