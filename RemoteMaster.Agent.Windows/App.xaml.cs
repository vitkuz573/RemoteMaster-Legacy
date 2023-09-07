using System.Diagnostics;
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

            ExecuteNetUse(@"\\SERVER-DC02\Win\RemoteMaster", "support@it-ktk.local", "teacher123!!");
            DirectoryCopy(@"\\SERVER-DC02\Win\RemoteMaster", $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster/Client");

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

    public static void ExecuteNetUse(string remotePath, string username, string password)
    {
        var startInfo = new ProcessStartInfo()
        {
            FileName = "cmd.exe",
            Arguments = $"/c net use {remotePath} {password} /user:{username}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
    }

    public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true)
    {
        if (!new DirectoryInfo(sourceDirName).Exists)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
        }

        if (AreAllFilesAndSubdirectoriesPresent(sourceDirName, destDirName))
        {
            return;
        }

        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        foreach (var file in new DirectoryInfo(sourceDirName).GetFiles())
        {
            var tempPath = Path.Combine(destDirName, file.Name);

            if (!File.Exists(tempPath))
            {
                file.CopyTo(tempPath, false);
            }
        }

        if (copySubDirs)
        {
            foreach (var subdir in new DirectoryInfo(sourceDirName).GetDirectories())
            {
                var tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }

    private bool AreAllFilesAndSubdirectoriesPresent(string sourceDir, string destDir)
    {
        var sourceDirectoryInfo = new DirectoryInfo(sourceDir);

        foreach (var file in sourceDirectoryInfo.GetFiles())
        {
            if (!File.Exists(Path.Combine(destDir, file.Name)))
            {
                return false;
            }
        }

        foreach (var subDir in sourceDirectoryInfo.GetDirectories())
        {
            var destSubDir = Path.Combine(destDir, subDir.Name);

            if (!Directory.Exists(destSubDir) || !AreAllFilesAndSubdirectoriesPresent(subDir.FullName, destSubDir))
            {
                return false;
            }
        }

        return true;
    }
}
