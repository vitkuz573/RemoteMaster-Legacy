using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text.Json;
using System.Windows;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent;

public partial class MainWindow : Window
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Agent";

    public MainWindow()
    {
        InitializeComponent();
        LoadConfigurationFromFile();
        CheckServiceStatusAndToggleUninstallButton();
    }

    private void LoadConfigurationFromFile()
    {
        var fileName = $"{AppDomain.CurrentDomain.FriendlyName}.json";

        if (File.Exists(fileName))
        {
            using var reader = new StreamReader(fileName);
            var json = reader.ReadToEnd();

            try
            {
                var config = JsonSerializer.Deserialize<ConfigurationModel>(json);
                
                if (config == null || string.IsNullOrWhiteSpace(config.Server) || string.IsNullOrWhiteSpace(config.Group))
                {
                    ShowErrorAndExit("Configuration is invalid. The application will now exit.");
                }
                else
                {
                    ServerAddressTextBlock.Text = $"Server Address: {config.Server}";
                    GroupTextBlock.Text = $"PC Group: {config.Group}";
                }
            }
            catch (JsonException)
            {
                ShowErrorAndExit("Error parsing the configuration file. The application will now exit.");
            }
        }
        else
        {
            ShowErrorAndExit("Configuration file not found. The application will now exit.");
        }
    }

    private static void ShowErrorAndExit(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current.Shutdown();
    }

    private void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        InstallWindowsService("RCService");
    }

    private static void InstallWindowsService(string serviceName)
    {
        var currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var newExecutablePath = Path.Combine(programFilesPath, MainAppName, $"{MainAppName}.{SubAppName}.exe");

        if (!File.Exists(newExecutablePath))
        {
            File.Copy(currentExecutablePath, newExecutablePath);
        }

        using var serviceController = new ServiceController(serviceName);
        
        if (ServiceController.GetServices().Any(s => s.ServiceName == serviceName))
        {
            return;
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "sc",
            Arguments = $"create {serviceName} binPath= \"{newExecutablePath}\" start= auto",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas"
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        process.Start();
        process.WaitForExit();
    }

    private void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        UninstallService("RCService");
    }

    private static void UninstallService(string serviceName)
    {
        var currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var newExecutablePath = Path.Combine(programFilesPath, MainAppName, $"{MainAppName}.{SubAppName}.exe");

        using var serviceController = new ServiceController(serviceName);
        
        if (ServiceController.GetServices().Any(s => s.ServiceName == serviceName))
        {
            if (serviceController.Status != ServiceControllerStatus.Stopped)
            {
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
            }

            var processStartInfoDelete = new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = $"delete {serviceName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var processDelete = new Process { StartInfo = processStartInfoDelete };
            processDelete.Start();
            processDelete.WaitForExit();
        }

        if (File.Exists(newExecutablePath))
        {
            File.Delete(newExecutablePath);
        }
        
        Directory.Delete(Path.Combine(programFilesPath, MainAppName), true);

        if (currentExecutablePath.Equals(newExecutablePath, StringComparison.InvariantCultureIgnoreCase))
        {
            Application.Current.Shutdown();
        }
    }

    private void CheckServiceStatusAndToggleUninstallButton()
    {
        var serviceName = "RCService";
        var serviceExists = ServiceController.GetServices().Any(s => s.ServiceName == serviceName);
        UninstallButton.IsEnabled = serviceExists;
    }
}
