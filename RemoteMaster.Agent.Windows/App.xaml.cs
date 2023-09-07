using System.ComponentModel;
using System.IO;
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
using Windows.Win32.NetworkManagement.WNet;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Agent;

public partial class App : Application
{
    private readonly IHost _host;

    public IServiceProvider ServiceProvider => _host.Services;

    private const string SharedFolder = @"\\SERVER-DC02\Win\RemoteMaster";
    private const string Login = "support@it-ktk.local";
    private const string Password = "teacher123!!";

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

            MapNetworkDrive(SharedFolder, Login, Password);
            DirectoryCopy(SharedFolder, $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster/Client");

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

    public static unsafe void MapNetworkDrive(string remotePath, string username, string password)
    {
        var netResource = new NETRESOURCEW
        {
            dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK
        };

        fixed (char* pRemotePath = remotePath)
        {
            netResource.lpRemoteName = pRemotePath;

            var result = WNetAddConnection2W(in netResource, password, username, 0);

            if (result != 0)
            {
                throw new Win32Exception((int)result);
            }
        }
    }

    public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true)
    {
        var sourceDir = new DirectoryInfo(sourceDirName);

        if (!sourceDir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
        }

        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        foreach (var file in sourceDir.GetFiles())
        {
            var destPath = Path.Combine(destDirName, file.Name);

            if (!File.Exists(destPath))
            {
                file.CopyTo(destPath, false);
            }
        }

        if (copySubDirs)
        {
            foreach (var subdir in sourceDir.GetDirectories())
            {
                var destSubDir = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, destSubDir, true);
            }
        }
    }
}
