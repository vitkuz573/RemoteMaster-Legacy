using System.IO;
using System.Net;
using System.Net.Sockets;
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

    private readonly IClientService _clientService;
    private readonly IServiceManager _serviceManager;
    private readonly string _hostName;
    private readonly string _ipv4Address;
    private readonly ConfigurationModel _configuration;

    public MainWindow()
    {
        InitializeComponent();

        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _clientService = serviceProvider.GetRequiredService<IClientService>();
        _serviceManager = serviceProvider.GetRequiredService<IServiceManager>();

        _hostName = Dns.GetHostName();
        _ipv4Address = GetIPv4Address(_hostName);

        _configuration = LoadConfigurationFromFile();
        DisplayConfigurationAndSystemInfo();
        UpdateServiceStatusDisplay();
    }

    private static string GetIPv4Address(string hostName)
    {
        var allAddresses = Dns.GetHostAddresses(hostName);

        return Array.Find(allAddresses, a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "Not found";
    }

    private void DisplayConfigurationAndSystemInfo()
    {
        HostNameTextBlock.Text = $"Host Name: {_hostName}";
        IPV4AddressTextBlock.Text = $"IPv4 Address: {_ipv4Address}";
        ServerAddressTextBlock.Text = $"Server Address: {_configuration.Server}";
        GroupTextBlock.Text = $"Group: {_configuration.Group}";
    }

    private static ConfigurationModel LoadConfigurationFromFile()
    {
        var fileName = GetConfigurationFileName();

        if (!TryReadFile(fileName, out var json))
        {
            ShowError("Configuration file not found.");

            return default;
        }

        if (!TryDeserializeJson(json, out var config) || !IsValidConfig(config))
        {
            ShowError("Error parsing or validating the configuration file.");
            
            return default;
        }

        return config;
    }

    private static string GetConfigurationFileName() => $"{AppDomain.CurrentDomain.FriendlyName}.json";

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

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current?.Shutdown();
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

        if (!_serviceManager.IsServiceInstalled())
        {
            _serviceManager.InstallService(newExecutablePath);
        }

        _serviceManager.StartService();
        UpdateServiceStatusDisplay();

        var registerResult = await _clientService.RegisterAsync(_configuration, _hostName, _ipv4Address);
        
        if (!registerResult)
        {
            ShowError("Client registration failed.");
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

    private void UpdateServiceStatusDisplay()
    {
        var serviceExists = _serviceManager.IsServiceInstalled();
        UninstallButton.IsEnabled = serviceExists;
        ServiceStatusTextBlock.Text = serviceExists ? "Service Status: Installed" : "Service Status: Not Installed";
    }

    private async void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviceManager.IsServiceInstalled())
        {
            var unregisterResult = await _clientService.UnregisterAsync(_configuration, _hostName);
            
            if (!unregisterResult)
            {
                ShowError("Client unregistration failed.");
            }

            _serviceManager.StopService();
            _serviceManager.UninstallService();
        }

        RemoveServiceFiles();
        UpdateServiceStatusDisplay();
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
}
