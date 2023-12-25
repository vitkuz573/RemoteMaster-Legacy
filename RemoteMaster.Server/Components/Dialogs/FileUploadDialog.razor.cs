// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class FileUploadDialog
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; } = default!;

    private IBrowserFile _selectedFile;
    private string _destinationPath;

    private async Task Upload()
    {
        FileUploadDto fileDto = null;

        if (_selectedFile != null)
        {
            var data = new byte[_selectedFile.Size];
            await _selectedFile.OpenReadStream().ReadAsync(data);

            fileDto = new FileUploadDto
            {
                Name = _selectedFile.Name,
                Data = data,
                DestinationPath = _destinationPath
            };

        }

        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("UploadFile", fileDto));

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void OnInputFileChange(InputFileChangeEventArgs e)
    {
        _selectedFile = e.File;
    }
}
