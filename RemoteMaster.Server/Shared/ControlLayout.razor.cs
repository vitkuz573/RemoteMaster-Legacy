// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Shared;

public partial class ControlLayout
{
    private bool _isMenuOpen = false;
    private string _clientVersion;
    private string _agentVersion;
    private bool _inputEnabled;
    private bool _cursorTracking;
    private int _quality;

    private string _rebootMessage = "test";
    private int _timeout = 60;

    private bool _firstToggleMenu = true;

    [Inject]
    private ControlFunctionsService ControlFunctionsService { get; set; }

    private async Task ToggleMenu()
    {
        await GetVersions();

        _isMenuOpen = !_isMenuOpen;

        if (_firstToggleMenu)
        {
            var clientConfiguration = ControlFunctionsService.ClientConfiguration;

            _inputEnabled = clientConfiguration.InputEnabled;
            _cursorTracking = clientConfiguration.TrackCursor;
            _quality = clientConfiguration.ImageQuality;

            _firstToggleMenu = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async void OnChangeScreen(ChangeEventArgs e)
    {
        await ControlFunctionsService.ControlHubProxy.SendSelectedScreen(e.Value.ToString());
    }

    private async void ChangeQuality(int quality)
    {
        _quality = quality;

        await ControlFunctionsService.ControlHubProxy.SetQuality(quality);
    }

    private async Task ToggleCursorTracking(bool value)
    {
        _cursorTracking = value;

        await ControlFunctionsService.ControlHubProxy.SetTrackCursor(value);
    }

    private async Task ToggleInputEnabled(bool value)
    {
        _inputEnabled = value;

        await ControlFunctionsService.ControlHubProxy.SetInputEnabled(value);
    }

    private async void KillClient()
    {
        await ControlFunctionsService.ControlHubProxy.KillClient();
    }

    private async void RebootComputer()
    {
        await ControlFunctionsService.ControlHubProxy.RebootComputer(_rebootMessage, _timeout, true);
    }

    private async void SendCtrlAltDel()
    {
        await ControlFunctionsService.AgentConnection.InvokeAsync("SendCtrlAltDel");
    }

    private async Task GetVersions()
    {
        if (ControlFunctionsService == null)
        {
            Console.WriteLine("ControlFunctionsService is null");
            return;
        }

        if (string.IsNullOrEmpty(ControlFunctionsService.Host))
        {
            Console.WriteLine("ControlFunctionsService.Host is null or empty");
            return;
        }

        var url = $"http://{ControlFunctionsService.Host}:5124/api/update/versions";
        Console.WriteLine($"URL: {url}");

        using var client = new HttpClient();

        try
        {
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {result}");

                if (string.IsNullOrEmpty(result))
                {
                    Console.WriteLine("Response content is null or empty");
                    return;
                }

                var versions = JsonSerializer.Deserialize<List<VersionInfo>>(result);

                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine("Deserialized versions list is null or empty");
                    return;
                }

                foreach (var version in versions)
                {
                    if (version.ComponentName.Equals("Agent", StringComparison.OrdinalIgnoreCase))
                    {
                        _agentVersion = version.CurrentVersion;
                    }
                    else if (version.ComponentName.Equals("Client", StringComparison.OrdinalIgnoreCase))
                    {
                        _clientVersion = version.CurrentVersion;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
}
