// Файл MainWindow.xaml.cs

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent;

public partial class MainWindow : Window
{
    private IServiceManager _serviceManager;
    private IConfigurationService _configurationService;
    private IHostInfoService _hostInfoService;
    private IAgentServiceManager _agentServiceManager;
    private IUpdaterServiceManager _updaterServiceManager;

    private string _hostName;
    private string _ipv4Address;
    private string _macAddress;
    private ConfigurationModel _configuration;

    public MainWindow()
    {
        var args = Environment.GetCommandLineArgs();

        InitializeServices();

        if (_configuration == null || string.IsNullOrEmpty(_configuration.Server))
        {
            MessageBox.Show("Configuration file is missing or invalid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        if (args.Contains("--install"))
        {
            InstallAndUpdate();
        }
        else
        {
            InitializeComponent();
            DisplayConfigurationAndSystemInfo();
            UpdateAgentStatusDisplay();
        }
    }

    private void InitializeServices()
    {
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _serviceManager = serviceProvider.GetRequiredService<IServiceManager>();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        _hostInfoService = serviceProvider.GetRequiredService<IHostInfoService>();
        _agentServiceManager = serviceProvider.GetRequiredService<IAgentServiceManager>();
        _updaterServiceManager = serviceProvider.GetRequiredService<IUpdaterServiceManager>();

        _agentServiceManager.MessageReceived += OnMessageReceived;
        _updaterServiceManager.MessageReceived += OnMessageReceived;

        _hostName = _hostInfoService.GetHostName();
        _ipv4Address = _hostInfoService.GetIPv4Address();
        _macAddress = _hostInfoService.GetMacAddress();

        try
        {
            _configuration = _configurationService.LoadConfiguration();
        }
        catch (Exception ex)
        {
            // Обработка исключений
            Application.Current.Shutdown();
            return;
        }
    }

    private async void InstallAndUpdate()
    {
        await _agentServiceManager.InstallOrUpdate(_configuration, _hostName, _ipv4Address, _macAddress);
        _updaterServiceManager.InstallOrUpdate();
        Application.Current.Shutdown();
    }

    private void OnMessageReceived(string message, MessageType type)
    {
        var args = Environment.GetCommandLineArgs();

        if (!args.Contains("--install"))
        {
            MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, type == MessageType.Error ? MessageBoxImage.Error : MessageBoxImage.Information);
        }
    }

    private void DisplayConfigurationAndSystemInfo()
    {
        HostNameValueTextBlock.Text = _hostName;
        IPV4AddressValueTextBlock.Text = _ipv4Address;
        MACAddressValueTextBlock.Text = _macAddress;
        ServerAddressValueTextBlock.Text = _configuration.Server;
        GroupValueTextBlock.Text = _configuration.Group;
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await InstallAndUpdateAsync();
    }

    private async Task InstallAndUpdateAsync()
    {
        await _agentServiceManager.InstallOrUpdate(_configuration, _hostName, _ipv4Address, _macAddress);
        _updaterServiceManager.InstallOrUpdate();

        UpdateAgentStatusDisplay();
    }

    private async void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        await _agentServiceManager.Uninstall(_configuration, _hostName);
        _updaterServiceManager.Uninstall();

        UpdateAgentStatusDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateAgentStatusDisplay()
    {
        var agentServiceExists = _serviceManager.IsServiceInstalled("RCService");
        var updaterServiceExists = _serviceManager.IsServiceInstalled("RCSUpdater");

        UninstallButton.IsEnabled = agentServiceExists || updaterServiceExists;
        AgentServiceStatusValueTextBlock.Text = agentServiceExists ? "Installed" : "Not Installed";
        UpdaterServiceStatusValueTextBlock.Text = updaterServiceExists ? "Installed" : "Not Installed";
    }
}
