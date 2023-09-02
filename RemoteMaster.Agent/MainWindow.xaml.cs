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
    private const string ServiceName = "RCService";

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
                ValidateAndSetConfig(config);
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

    private void ValidateAndSetConfig(ConfigurationModel? config)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.Server) || string.IsNullOrWhiteSpace(config.Group))
        {
            ShowErrorAndExit("Configuration is invalid. The application will now exit.");
        }
        else
        {
            ServerAddressTextBlock.Text = $"Server Address: {config.Server}";
            GroupTextBlock.Text = $"Group: {config.Group}";
        }
    }

    private static void ShowErrorAndExit(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current.Shutdown();
    }

    private void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        InstallWindowsService();
    }

    private static void InstallWindowsService()
    {
        var newExecutablePath = GetNewExecutablePath();
        EnsureFileIsCopied(newExecutablePath);

        if (!IsServiceInstalled(ServiceName))
        {
            ExecuteServiceCommand($"create {ServiceName} binPath= \"{newExecutablePath}\" start= auto");
        }
    }

    private static void UninstallService()
    {
        var newExecutablePath = GetNewExecutablePath();

        if (IsServiceInstalled(ServiceName))
        {
            StopAndRemoveService(ServiceName);
        }

        RemoveServiceFiles(newExecutablePath);
    }

    private static string GetNewExecutablePath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return Path.Combine(programFilesPath, MainAppName, SubAppName, $"{MainAppName}.{SubAppName}.exe");
    }

    private static void EnsureFileIsCopied(string newExecutablePath)
    {
        if (!File.Exists(newExecutablePath))
        {
            var newDirectoryPath = Path.GetDirectoryName(newExecutablePath);
            
            if (newDirectoryPath != null && !Directory.Exists(newDirectoryPath))
            {
                Directory.CreateDirectory(newDirectoryPath);
            }

            var currentExecutablePath = Environment.ProcessPath;
            File.Copy(currentExecutablePath, newExecutablePath);
        }
    }

    private static bool IsServiceInstalled(string serviceName)
    {
        return ServiceController.GetServices().Any(s => s.ServiceName == serviceName);
    }

    private static void ExecuteServiceCommand(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "sc",
            Arguments = arguments,
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

    private static void StopAndRemoveService(string serviceName)
    {
        using var serviceController = new ServiceController(serviceName);

        if (serviceController.Status != ServiceControllerStatus.Stopped)
        {
            serviceController.Stop();
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        ExecuteServiceCommand($"delete {serviceName}");
    }

    private static void RemoveServiceFiles(string newExecutablePath)
    {
        if (File.Exists(newExecutablePath))
        {
            File.Delete(newExecutablePath);
        }

        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        Directory.Delete(Path.Combine(programFilesPath, MainAppName), true);
    }

    private void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        UninstallService();
    }

    private void CheckServiceStatusAndToggleUninstallButton()
    {
        var serviceExists = IsServiceInstalled(ServiceName);
        UninstallButton.IsEnabled = serviceExists;

        if (!serviceExists)
        {
            ServiceStatusTextBlock.Text = "Service Status: Not Installed";
            return;
        }

        using var serviceController = new ServiceController(ServiceName);

        ServiceStatusTextBlock.Text = serviceController.Status switch
        {
            ServiceControllerStatus.Running => "Service Status: Running",
            _ => "Service Status: Not Running",
        };
    }
}
