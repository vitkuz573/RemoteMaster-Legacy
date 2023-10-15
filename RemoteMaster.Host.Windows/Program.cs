// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Claims;
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
var isUserInstanceMode = args.Contains("--user-instance");
var isInstallMode = args.Contains("--install");
var isUninstallMode = args.Contains("--uninstall");

if (new[] { isServiceMode, isUserInstanceMode, isInstallMode, isUninstallMode }.Count(val => val) > 1)
{
    Console.WriteLine("[ERROR] Arguments --install, --uninstall, --service-mode and --user-instance are mutually exclusive. Please specify only one.");

    return;
}

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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
                var localIPv6Mapped = IPAddress.Parse("::ffff:127.0.0.1");

                if (remoteIp.Equals(IPAddress.Loopback) || remoteIp.Equals(IPAddress.IPv6Loopback) || remoteIp.Equals(localIPv6Mapped))
                {
                    Console.WriteLine("Localhost detected");
                    var identity = new ClaimsIdentity();
                    
                    context.Principal = new ClaimsPrincipal(identity);
                    context.Success();
                }

                return Task.CompletedTask;
            }
        };
    });

if (!isServiceMode)
{
    builder.ConfigureCoreUrls();
}
else
{
    builder.Services.AddHostedService<HiddenWindowService>();
    builder.Services.AddHostedService<ServiceCommandListener>();
}

var app = builder.Build();

if (isInstallMode)
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
        Console.WriteLine($"[ERROR] Configuration file not found: {ex.Message}");

        return;
    }
    catch (InvalidDataException ex)
    {
        Console.WriteLine($"[ERROR] Invalid configuration data: {ex.Message}");

        return;
    }

    var hostName = hostInfoService.GetHostName();
    var ipv4Address = hostInfoService.GetIPv4Address();
    var macAddress = hostInfoService.GetMacAddress();

    Console.WriteLine(new string('=', 40));
    Console.WriteLine("INSTALLATION DETAILS:");
    Console.WriteLine(new string('-', 40));
    Console.WriteLine($"Server: {configuration.Server}");
    Console.WriteLine($"Group: {configuration.Group}");
    Console.WriteLine(new string('-', 40));
    Console.WriteLine("HOST INFORMATION:");
    Console.WriteLine($"Host Name: {hostName}");
    Console.WriteLine($"IPv4 Address: {ipv4Address}");
    Console.WriteLine($"MAC Address: {macAddress}");
    Console.WriteLine(new string('=', 40));

    await hostServiceManager.InstallOrUpdate(configuration, hostName, ipv4Address, macAddress);

    return;
}

if (isUninstallMode)
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
        Console.WriteLine($"[ERROR] Configuration file not found: {ex.Message}");

        return;
    }
    catch (InvalidDataException ex)
    {
        Console.WriteLine($"[ERROR] Invalid configuration data: {ex.Message}");

        return;
    }

    var hostName = hostInfoService.GetHostName();

    await hostServiceManager.Uninstall(configuration, hostName);

    return;
}

if (isServiceMode)
{
    var hostService = app.Services.GetRequiredService<IHostService>();

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
