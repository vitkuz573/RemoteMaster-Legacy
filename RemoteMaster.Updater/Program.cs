// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory
});

builder.Host.UseWindowsService();

builder.Services.AddControllers();

builder.Services.AddScoped<IComponentUpdater, ClientComponentUpdater>();
builder.Services.AddScoped<IComponentUpdater, AgentComponentUpdater>();

// Configure HTTPS with the certificate
using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
store.Open(OpenFlags.ReadOnly);

var certificate = store.Certificates
    .OfType<X509Certificate2>()
    .First(c => c.Thumbprint == "5B7E57CCB01A4BFE780F0B6A05B0C71B77FDD29F");

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5124, listenOptions =>
    {
        listenOptions.UseHttps(new HttpsConnectionAdapterOptions
        {
            ServerCertificate = certificate,
        });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapControllers();
app.UseRouting();
app.UseAuthorization();
app.Run();
