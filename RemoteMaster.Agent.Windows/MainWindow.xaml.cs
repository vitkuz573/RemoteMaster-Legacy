// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Agent;

public partial class MainWindow : Window
{
    private readonly IServiceManager _serviceManager;
    private readonly IConfigurationService _configurationService;
    private readonly IHostInfoProvider _hostInfoProvider;
    private readonly IAgentServiceManager _agentServiceManager;
    private readonly IUpdaterServiceManager _updaterServiceManager;
    private readonly AgentServiceConfigProvider _agentServiceConfig;

    private readonly string _hostName;
    private readonly string _ipv4Address;
    private readonly string _macAddress;
    private readonly ConfigurationModel _configuration;

    public MainWindow()
    {
        InitializeComponent();

        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _serviceManager = serviceProvider.GetRequiredService<IServiceManager>();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        _hostInfoProvider = serviceProvider.GetRequiredService<IHostInfoProvider>();
        _agentServiceManager = serviceProvider.GetRequiredService<IAgentServiceManager>();
        _updaterServiceManager = serviceProvider.GetRequiredService<IUpdaterServiceManager>();
        _agentServiceConfig = serviceProvider.GetRequiredService<AgentServiceConfigProvider>();

        _agentServiceManager.MessageReceived += OnMessageReceived;
        _updaterServiceManager.MessageReceived += OnMessageReceived;

        _hostName = _hostInfoProvider.GetHostName();
        _ipv4Address = _hostInfoProvider.GetIPv4Address();
        _macAddress = GetMacAddressForIp(_ipv4Address);
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
        MACAddressTextBlock.Text = $"MAC Address: {Regex.Replace(_macAddress, "(..)(?!$)", "$1:")}";
        ServerAddressTextBlock.Text = $"Server Address: {_configuration.Server}";
        GroupTextBlock.Text = $"Group: {_configuration.Group}";
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await _agentServiceManager.InstallOrUpdate(_configuration, _hostName, _ipv4Address, _macAddress);
        await _updaterServiceManager.InstallOrUpdate();

        UpdateServiceStatusDisplay();
    }

    private async void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        await _agentServiceManager.Uninstall(_configuration, _hostName);
        await _updaterServiceManager.Uninstall();

        UpdateServiceStatusDisplay();
    }

    public static string GetMacAddressForIp(string ipAddress)
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            var ipProperties = nic.GetIPProperties();

            foreach (var ip in ipProperties.UnicastAddresses)
            {
                if (ip.Address.ToString() == ipAddress)
                {
                    return nic.GetPhysicalAddress().ToString();
                }
            }
        }

        return string.Empty;
    }

    private void UpdateServiceStatusDisplay()
    {
        var serviceExists = _serviceManager.IsServiceInstalled(_agentServiceConfig.ServiceName);
        UninstallButton.IsEnabled = serviceExists;
        ServiceStatusTextBlock.Text = serviceExists ? "Service Status: Installed" : "Service Status: Not Installed";
    }
}
