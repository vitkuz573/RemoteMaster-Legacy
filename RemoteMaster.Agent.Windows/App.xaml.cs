using System.IO;
using System.Security.Cryptography;
using System.Windows;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Agent;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Core.Extensions;
using RemoteMaster.Agent.Helpers;
using RemoteMaster.Agent.Models;
using RemoteMaster.Agent.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Services;

var builder = WebApplication.CreateBuilder().ConfigureCoreUrls();

builder.Host.UseContentRoot(AppContext.BaseDirectory);
builder.Host.UseWindowsService();

builder.Services.AddCoreServices();
builder.Services.AddSingleton<IClientService, ClientService>();
builder.Services.AddSingleton<IAgentServiceManager, AgentServiceManager>();
builder.Services.AddSingleton<IUpdaterServiceManager, UpdaterServiceManager>();
builder.Services.AddSingleton<IServiceManager, ServiceManager>();
builder.Services.AddSingleton<MainWindow>();
builder.Services.AddSingleton<AgentServiceConfig>();
builder.Services.AddSingleton<UpdaterServiceConfig>();
builder.Services.AddSingleton<IDictionary<string, IServiceConfig>>(sp => new Dictionary<string, IServiceConfig>
{
    { "agent", sp.GetRequiredService<AgentServiceConfig>() },
    { "updater", sp.GetRequiredService<UpdaterServiceConfig>() }
});

builder.Services.AddSingleton<IScreenCapturerService, BitBltCapturer>();
builder.Services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
builder.Services.AddSingleton<ICursorRenderService, CursorRenderService>();
builder.Services.AddSingleton<IInputService, InputService>();
builder.Services.AddSingleton<IPowerService, PowerService>();
builder.Services.AddSingleton<IHardwareService, HardwareService>();
builder.Services.AddSingleton<IServiceManager, ServiceManager>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapCoreHubs();

app.Run();

public partial class App : Application
{
    private WebApplication _host;

    public IServiceProvider ServiceProvider => _host.Services;
}
