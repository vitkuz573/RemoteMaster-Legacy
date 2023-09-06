using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Core.Extensions;
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
                services.AddCoreServices();
                services.AddSingleton<IConfigurationService, ConfigurationService>();
                services.AddSingleton<IHostInfoProvider, HostInfoProvider>();
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
                    });
                })
                .UseWindowsService()
                .Build();

            _host.StartAsync();

            MonitorClient();
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

    private async void MonitorClient()
    {
        var clientService = ServiceProvider.GetRequiredService<IClientService>();

        while (true)
        {
            if (!clientService.IsClientRunning())
            {
                clientService.StartClient();
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
