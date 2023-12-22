// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Components.Dialogs;

#pragma warning disable CA1822

public partial class FileManager
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; } = default!;

    private List<string> Files { get; set; } = [];

    private string _currentPath = @"C:\";

    protected async override Task OnInitializedAsync()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) =>
        {
            connection.On<List<string>>("ReceiveFiles", (files) =>
            {
                Files = files;
                StateHasChanged();
            });

            await connection.InvokeAsync("GetFiles", _currentPath);
        });
    }

    private async Task Refresh()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("GetFiles", _currentPath));
    }

    private async Task DownloadFile(string fileName)
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("DownloadFile", $"{_currentPath}/{fileName}"));
    }

    private Task Upload()
    {
        return Task.CompletedTask;
    }
}
