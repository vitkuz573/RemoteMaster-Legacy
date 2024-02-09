// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class HostConfigurationGenerator
{
    private readonly HostConfiguration _model = new();

    protected override void OnInitialized()
    {
        _model.Server = GetLocalIpAddress();
        _model.Subject = new();
    }

    private async Task OnValidSubmit(EditContext context)
    {
        await JSRuntime.InvokeVoidAsync("generateAndDownloadFile", _model);

        StateHasChanged();
    }

    public void DownloadHost()
    {
        NavigationManager.NavigateTo("api/HostConfiguration/download-host", true);
    }

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}
