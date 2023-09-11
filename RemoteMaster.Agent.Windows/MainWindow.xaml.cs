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
    private readonly IServiceManager _serviceManager;
    private readonly IConfigurationService _configurationService;
    private readonly IHostInfoProvider _hostInfoProvider;
    private readonly IAgentServiceManager _agentServiceManager;

    private readonly string _hostName;
    private readonly string _ipv4Address;
    private readonly ConfigurationModel _configuration;

    public MainWindow()
    {
        InitializeComponent();

        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _serviceManager = serviceProvider.GetRequiredService<IServiceManager>();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        _hostInfoProvider = serviceProvider.GetRequiredService<IHostInfoProvider>();
        _agentServiceManager = serviceProvider.GetRequiredService<IAgentServiceManager>();
        
        _agentServiceManager.MessageReceived += OnMessageReceived;

        _hostName = _hostInfoProvider.GetHostName();
        _ipv4Address = _hostInfoProvider.GetIPv4Address();
        _configuration = _configurationService.LoadConfiguration();

        DisplayConfigurationAndSystemInfo();
        UpdateServiceStatusDisplay();
    }

    private void OnMessageReceived(string message, MessageType type)
    {
        MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, type == MessageType.Error ? MessageBoxImage.Error : MessageBoxImage.Information);
    }

    private void DisplayConfigurationAndSystemInfo()
    {
        HostNameTextBlock.Text = $"Host Name: {_hostName}";
        IPV4AddressTextBlock.Text = $"IPv4 Address: {_ipv4Address}";
        ServerAddressTextBlock.Text = $"Server Address: {_configuration.Server}";
        GroupTextBlock.Text = $"Group: {_configuration.Group}";
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await _agentServiceManager.InstallOrUpdateService(_configuration, _hostName, _ipv4Address);

        UpdateServiceStatusDisplay();
    }

    private async void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        await _agentServiceManager.UninstallService(_configuration, _hostName);

        UpdateServiceStatusDisplay();
    }

    private void UpdateServiceStatusDisplay()
    {
        var serviceExists = _serviceManager.IsServiceInstalled();
        UninstallButton.IsEnabled = serviceExists;
        ServiceStatusTextBlock.Text = serviceExists ? "Service Status: Installed" : "Service Status: Not Installed";
    }
}
