// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class FileUploadDialog
{
    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full z-10";
    private string _dragClass = DefaultDragClass;
    private List<IBrowserFile> _files = [];
    private string _destinationPath = string.Empty;

    private async Task Clear()
    {
        _files.Clear();
        ClearDragClass();
        await Task.Delay(100);
    }

    private void OnInputFileChanged(InputFileChangeEventArgs e)
    {
        ClearDragClass();

        _files = [.. e.GetMultipleFiles()];
    }

    private async Task Upload()
    {
        FileUploadDto? fileDto = null;

        if (_files != null)
        {
            foreach (var file in _files)
            {
                var data = new byte[file.Size];
                await file.OpenReadStream(file.Size).ReadAsync(data);

                fileDto = new FileUploadDto
                {
                    Name = file.Name,
                    Data = data,
                    DestinationPath = _destinationPath
                };

                await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("UploadFile", fileDto));
            }
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void SetDragClass() => _dragClass = $"{DefaultDragClass} mud-border-primary";

    private void ClearDragClass() => _dragClass = DefaultDragClass;
}
