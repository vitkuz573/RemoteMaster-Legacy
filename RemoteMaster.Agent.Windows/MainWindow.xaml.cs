// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO;
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
    private readonly IHostInfoService _hostInfoService;
    private readonly IAgentServiceManager _agentServiceManager;
    private readonly IUpdaterServiceManager _updaterServiceManager;

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
        catch (FileNotFoundException)
        {
            MessageBox.Show("Configuration file not found. Please check your configuration.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }
        catch (InvalidDataException)
        {
            MessageBox.Show("Error parsing or validating the configuration file. Please check your configuration.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        DisplayConfigurationAndSystemInfo();
        UpdateAgentStatusDisplay();
    }

    private void OnMessageReceived(string message, MessageType type)
    {
        MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, type == MessageType.Error ? MessageBoxImage.Error : MessageBoxImage.Information);
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
        
        UninstallButton.IsEnabled = agentServiceExists;
        AgentServiceStatusValueTextBlock.Text = agentServiceExists ? "Installed" : "Not Installed";
        UpdaterServiceStatusValueTextBlock.Text = updaterServiceExists ? "Installed" : "Not Installed";
    }
}
