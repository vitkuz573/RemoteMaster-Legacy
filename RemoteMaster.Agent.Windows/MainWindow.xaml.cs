using System.IO;
using System.Net;
using System.Net.Sockets;
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
    private readonly IConfigurationService _configurationService;
    private readonly IHostInfoProvider _hostInfoProvider;
    private readonly string _hostName;
    private readonly string _ipv4Address;
    private readonly ConfigurationModel _configuration;

    public MainWindow()
    {
        InitializeComponent();

        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _clientService = serviceProvider.GetRequiredService<IClientService>();
        _serviceManager = serviceProvider.GetRequiredService<IServiceManager>();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        _hostInfoProvider = serviceProvider.GetRequiredService<IHostInfoProvider>();

        _hostName = _hostInfoProvider.GetHostName();
        _ipv4Address = _hostInfoProvider.GetIPv4Address();

        _configuration = _configurationService.LoadConfiguration();
        DisplayConfigurationAndSystemInfo();
        UpdateServiceStatusDisplay();
    }

    private void DisplayConfigurationAndSystemInfo()
    {
        HostNameTextBlock.Text = $"Host Name: {_hostName}";
        IPV4AddressTextBlock.Text = $"IPv4 Address: {_ipv4Address}";
        ServerAddressTextBlock.Text = $"Server Address: {_configuration.Server}";
        GroupTextBlock.Text = $"Group: {_configuration.Group}";
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

    private void CopyExecutableToNewPath(string newExecutablePath)
    {
        var newDirectoryPath = Path.GetDirectoryName(newExecutablePath);
        
        if (newDirectoryPath != null && !Directory.Exists(newDirectoryPath))
        {
            Directory.CreateDirectory(newDirectoryPath);
        }

        var currentExecutablePath = Environment.ProcessPath;
        File.Copy(currentExecutablePath, newExecutablePath);

        var configName = _configurationService.GetConfigurationFileName();
        var newConfigPath = Path.Combine(newDirectoryPath, configName);

        if (File.Exists(configName))
        {
            File.Copy(configName, newConfigPath, true);
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
