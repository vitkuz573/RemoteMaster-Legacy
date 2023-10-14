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
builder.Services.AddSingleton<IAgentServiceManager, AgentServiceManager>();
builder.Services.AddSingleton<IUpdaterServiceManager, UpdaterServiceManager>();
builder.Services.AddSingleton<IServiceManager, ServiceManager>();
builder.Services.AddSingleton<AgentServiceConfig>();
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
    { "agent", sp.GetRequiredService<AgentServiceConfig>() },
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

var app = builder.Build();

var hiddenWindow = app.Services.GetRequiredService<HiddenWindow>();

if (!args.Contains("--service-mode"))
{
    builder.ConfigureCoreUrls();
}
else
{
    hiddenWindow.RunMessageLoop();

    var processOptions = new ProcessStartOptions($"{Environment.ProcessPath} --user-instance", -1)
    {
        ForceConsoleSession = true,
        DesktopName = "default",
        HiddenWindow = false,
        UseCurrentUserToken = false
    };

    using var _ = NativeProcess.Start(processOptions);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCoreHubs();

app.Run();