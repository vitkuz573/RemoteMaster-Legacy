using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Core.Extensions;
using RemoteMaster.Agent.Core.Models;
using RemoteMaster.Agent.Services;

namespace RemoteMaster.Agent;

public partial class App : Application
{
    private readonly IHost _host;

    public IServiceProvider ServiceProvider => _host.Services;

    public App()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddCoreServices(hostContext.Configuration);
                services.AddSingleton<IClientService, ClientService>();
                services.AddSingleton<IServiceManager, ServiceManager>();
                services.AddSingleton<ISignatureService, SignatureService>();
                services.AddSingleton<IProcessService, ProcessService>();
                services.AddSingleton<MainWindow>();
            });

        if (WindowsServiceHelpers.IsWindowsService())
        {
            _host = hostBuilder
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(3564);
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapCoreHubs();
                        });
                    });
                })
                .UseWindowsService()
                .Build();

            _host.StartAsync();

            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            var clientSettings = _host.Services.GetRequiredService<IOptions<ClientSettings>>().Value;

            logger.LogInformation("Client settings: Path = {Path}, CertificateThumbprint = {Thumbprint}", clientSettings.Path, clientSettings.CertificateThumbprint);
        }
        else
        {
            _host = hostBuilder.Build();
            _host.StartAsync();

            MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        _host?.StopAsync().Wait();
    }
}
