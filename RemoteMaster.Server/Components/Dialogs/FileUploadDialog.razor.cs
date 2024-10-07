// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class FileUploadDialog
{
    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full z-10";
    private string _dragClass = DefaultDragClass;
    private List<IBrowserFile> _files = [];
    private string _destinationPath = string.Empty;

    private void OnInputFileChanged(InputFileChangeEventArgs e)
    {
        ClearDragClass();

        _files = [.. e.GetMultipleFiles()];
    }

    private async Task Upload()
    {
        FileUploadDto? fileDto;

        foreach (var file in _files)
        {
            await using var stream = file.OpenReadStream(file.Size);
            
            var data = new byte[file.Size];
            int bytesRead;
            var totalBytesRead = 0;

            while ((bytesRead = await stream.ReadAsync(data, totalBytesRead, data.Length - totalBytesRead)) > 0)
            {
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead != data.Length)
            {
                throw new InvalidOperationException("Could not read the entire file.");
            }

            fileDto = new FileUploadDto(file.Name, data, _destinationPath);

            await HostCommandService.Execute(Hosts, async (_, connection) => await connection!.InvokeAsync("UploadFile", fileDto));
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void SetDragClass() => _dragClass = $"{DefaultDragClass} mud-border-primary";

    private void ClearDragClass() => _dragClass = DefaultDragClass;
}
