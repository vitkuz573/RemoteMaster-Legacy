// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Host;
using RemoteMaster.Host.Abstractions;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Helpers;
using RemoteMaster.Host.Models;
using RemoteMaster.Host.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Services;

var isServiceMode = args.Contains("--service-mode");

var options = new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory,
    Args = args
};

var builder = WebApplication.CreateBuilder(options);

builder.Host.UseContentRoot(AppContext.BaseDirectory);
builder.Host.UseWindowsService();

builder.Services.AddCoreServices();
builder.Services.AddSingleton<IHostService, HostService>();
builder.Services.AddSingleton<IHostServiceManager, HostServiceManager>();
builder.Services.AddSingleton<IUpdaterServiceManager, UpdaterServiceManager>();
builder.Services.AddSingleton<IServiceManager, ServiceManager>();
builder.Services.AddSingleton<HostServiceConfig>();
builder.Services.AddSingleton<UpdaterServiceConfig>();
builder.Services.AddSingleton<IScreenCapturerService, BitBltCapturer>();
builder.Services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
builder.Services.AddSingleton<ICursorRenderService, CursorRenderService>();
builder.Services.AddSingleton<IInputService, InputService>();
builder.Services.AddSingleton<IPowerService, PowerService>();
builder.Services.AddSingleton<IHardwareService, HardwareService>();
builder.Services.AddSingleton<HiddenWindow>();

builder.Services.AddSingleton<IDictionary<string, IServiceConfig>>(sp => new Dictionary<string, IServiceConfig>
{
    { "host", sp.GetRequiredService<HostServiceConfig>() },
    { "updater", sp.GetRequiredService<UpdaterServiceConfig>() }
});

var publicKeyPath = @"C:\RemoteMaster\Security\public_key.pem";
var publicKey = File.ReadAllText(publicKeyPath);

using var rsa = new RSACryptoServiceProvider();
rsa.ImportFromPem(publicKey.ToCharArray());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = "RemoteMaster Server",
            ValidAudience = "RMServiceAPI",
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };
    });

if (!isServiceMode)
{
    builder.ConfigureCoreUrls();
}

var app = builder.Build();

if (args.Contains("--install"))
{
    var configurationService = app.Services.GetRequiredService<IConfigurationService>();
    var hostInfoService = app.Services.GetRequiredService<IHostInfoService>();
    var hostServiceManager = app.Services.GetRequiredService<IHostServiceManager>();

    ConfigurationModel configuration;

    try
    {
        configuration = configurationService.LoadConfiguration();
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine("Error: " + ex.Message);

        return;
    }
    catch (InvalidDataException ex)
    {
        Console.WriteLine("Error: " + ex.Message);

        return;
    }

    var hostName = hostInfoService.GetHostName();
    var ipv4Address = hostInfoService.GetIPv4Address();
    var macAddress = hostInfoService.GetMacAddress();

    Console.WriteLine($"Host Name: {hostName}");
    Console.WriteLine($"IPv4 Address: {ipv4Address}");
    Console.WriteLine($"MAC Address: {macAddress}");

    await hostServiceManager.InstallOrUpdate(configuration, hostName, ipv4Address, macAddress);
   
    return;
}

var hiddenWindow = app.Services.GetRequiredService<HiddenWindow>();
var hostService = app.Services.GetRequiredService<IHostService>();

if (isServiceMode)
{
    hiddenWindow.Initialize();
    hostService.Start();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCoreHubs();

app.Run();
