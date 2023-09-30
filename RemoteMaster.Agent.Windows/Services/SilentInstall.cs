// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO;
using System.Windows;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent.Services;

public class SilentInstall
{
    private readonly IConfigurationService _configurationService;
    private readonly IHostInfoService _hostInfoService;
    private readonly IAgentServiceManager _agentServiceManager;
    private readonly IUpdaterServiceManager _updaterServiceManager;

    private readonly string _hostName;
    private readonly string _ipv4Address;
    private readonly string _macAddress;
    private readonly ConfigurationModel _configuration;

    public SilentInstall(IConfigurationService configurationService, IHostInfoService hostInfoService, IAgentServiceManager agentServiceManager, IUpdaterServiceManager updaterServiceManager)
    {
        _configurationService = configurationService;
        _hostInfoService = hostInfoService;
        _agentServiceManager = agentServiceManager;
        _updaterServiceManager = updaterServiceManager;

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
            Console.WriteLine("Configuration file not found. Please check your configuration.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }
        catch (InvalidDataException)
        {
            Console.WriteLine("Error parsing or validating the configuration file. Please check your configuration.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }
    }

    private void OnMessageReceived(string message, MessageType type)
    {
        MessageBox.Show($"{type}: {message}");
    }

    public async Task Install()
    {
        await _agentServiceManager.InstallOrUpdate(_configuration, _hostName, _ipv4Address, _macAddress);
        _updaterServiceManager.InstallOrUpdate();
    }
}
