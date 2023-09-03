using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent;

public partial class MainWindow : Window
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Agent";
    private const string ServiceName = "RCService";
    private const string ServiceDisplayName = "Remote Control Service";

    private readonly IClientService _clientService;

    public MainWindow()
    {
        InitializeComponent();

        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _clientService = serviceProvider.GetRequiredService<IClientService>();

        LoadConfiguration();
        UpdateServiceStatusDisplay();
    }

    private void LoadConfiguration()
    {
        var config = LoadConfigurationFromFile();
        SetConfiguration(config);
    }

    private static ConfigurationModel LoadConfigurationFromFile()
    {
        var fileName = GetConfigurationFileName();

        if (!TryReadFile(fileName, out var json))
        {
            ShowErrorWithExit("Configuration file not found.");
        }

        if (!TryDeserializeJson(json, out var config) || !IsValidConfig(config))
        {
            ShowErrorWithExit("Error parsing or validating the configuration file.");
        }

        return config!;
    }

    private static string GetConfigurationFileName()
    {
        return $"{AppDomain.CurrentDomain.FriendlyName}.json";
    }

    private static bool TryReadFile(string fileName, out string content)
    {
        if (File.Exists(fileName))
        {
            using var reader = new StreamReader(fileName);
            content = reader.ReadToEnd();

            return true;
        }

        content = string.Empty;

        return false;
    }

    private static bool TryDeserializeJson(string json, out ConfigurationModel? config)
    {
        try
        {
            config = JsonSerializer.Deserialize<ConfigurationModel>(json);
            
            return true;
        }
        catch (JsonException)
        {
            config = null;

            return false;
        }
    }

    private static bool IsValidConfig(ConfigurationModel? config)
    {
        return config != null && !string.IsNullOrWhiteSpace(config.Server) && !string.IsNullOrWhiteSpace(config.Group);
    }

    private void SetConfiguration(ConfigurationModel config)
    {
        ServerAddressTextBlock.Text = $"Server Address: {config.Server}";
        GroupTextBlock.Text = $"Group: {config.Group}";
    }

    private static void ShowErrorWithExit(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current.Shutdown();
    }

    private void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        InstallOrUpdateService();
    }

    private async Task InstallOrUpdateService()
    {
        var newExecutablePath = GetNewExecutablePath();

        if (!File.Exists(newExecutablePath))
        {
            CopyExecutableToNewPath(newExecutablePath);
        }

        if (!IsServiceInstalled())
        {
            CreateService(newExecutablePath);
        }

        StartService();
        UpdateServiceStatusDisplay();

        var config = LoadConfigurationFromFile();
        var hostName = Dns.GetHostName();
        var allAddresses = Dns.GetHostAddresses(hostName);
        var ipv4Address = Array.Find(allAddresses, a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString();

        var registerResult = await _clientService.RegisterAsync(config, hostName, ipv4Address, config.Group);

        if (!registerResult)
        {
            ShowErrorWithExit("Client registration failed.");
        }
    }

    private static string GetNewExecutablePath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        
        return Path.Combine(programFilesPath, MainAppName, SubAppName, $"{MainAppName}.{SubAppName}.exe");
    }

    private static void CopyExecutableToNewPath(string newExecutablePath)
    {
        var newDirectoryPath = Path.GetDirectoryName(newExecutablePath);

        if (newDirectoryPath != null && !Directory.Exists(newDirectoryPath))
        {
            Directory.CreateDirectory(newDirectoryPath);
        }

        var currentExecutablePath = Environment.ProcessPath;
        File.Copy(currentExecutablePath, newExecutablePath);

        var currentConfigPath = GetConfigurationFileName();
        var newConfigPath = Path.Combine(newDirectoryPath, GetConfigurationFileName());
        
        if (File.Exists(currentConfigPath))
        {
            File.Copy(currentConfigPath, newConfigPath, true);
        }
    }

    private static bool IsServiceInstalled()
    {
        return ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);
    }

    private static void CreateService(string newExecutablePath)
    {
        ExecuteServiceCommand($"create {ServiceName} DisplayName= \"{ServiceDisplayName}\" binPath= \"{newExecutablePath}\" start= auto");
    }

    private static void StartService()
    {
        using var serviceController = new ServiceController(ServiceName);
        
        try
        {
            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running);
            }
        }
        catch (InvalidOperationException ex)
        {
            ShowErrorWithExit($"Unable to start the service. Detailed error: {ex.Message}");
        }
    }

    private void UpdateServiceStatusDisplay()
    {
        var serviceExists = IsServiceInstalled();
        UninstallButton.IsEnabled = serviceExists;
        ServiceStatusTextBlock.Text = serviceExists ? "Service Status: Installed" : "Service Status: Not Installed";
    }

    private void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsServiceInstalled())
        {
            StopService();
            RemoveService();
        }

        RemoveServiceFiles();
        UpdateServiceStatusDisplay();
    }

    private static void StopService()
    {
        using var serviceController = new ServiceController(ServiceName);

        if (serviceController.Status != ServiceControllerStatus.Stopped)
        {
            serviceController.Stop();
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
        }
    }

    private static void RemoveService()
    {
        ExecuteServiceCommand($"delete {ServiceName}");
    }

    private static void RemoveServiceFiles()
    {
        var newExecutablePath = GetNewExecutablePath();

        if (File.Exists(newExecutablePath))
        {
            File.Delete(newExecutablePath);
        }

        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var fullPath = Path.Combine(programFilesPath, MainAppName);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
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
}
