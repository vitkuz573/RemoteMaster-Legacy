// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
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

    private void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;

        if (_firstToggleMenu)
        {
            var clientConfiguration = ControlFunctionsService.ClientConfiguration;

            _inputEnabled = clientConfiguration.InputEnabled;
            _cursorTracking = clientConfiguration.TrackCursor;
            _quality = clientConfiguration.ImageQuality;

            _firstToggleMenu = false;
        }
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
}
