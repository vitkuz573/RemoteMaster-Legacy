// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Compression;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ScriptExecutorDialog
{
    private string _content = string.Empty;
    private Shell _shell = Shell.Cmd;
    private bool _asSystem;
    private readonly Dictionary<Computer, ComputerResults> _resultsPerComputer = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private async Task RunScript()
    {
        foreach (var (computer, connection) in Hosts)
        {
            if (connection != null && !_subscribedConnections.Contains(connection))
            {
                connection.On<Message>("ReceiveMessage", async message =>
                {
                    UpdateResultsForComputer(computer, message);
                    await InvokeAsync(StateHasChanged);
                });

                _subscribedConnections.Add(connection);
            }

            var scriptExecutionRequest = new ScriptExecutionRequest(_content, _shell)
            {
                AsSystem = _asSystem
            };

            if (connection != null)
            {
                await connection.InvokeAsync("SendScript", scriptExecutionRequest);
            }
        }
    }

    private void UpdateResultsForComputer(Computer computer, Message scriptResult)
    {
        if (!_resultsPerComputer.TryGetValue(computer, out var results))
        {
            results = new ComputerResults();
            _resultsPerComputer[computer] = results;
        }

        if (scriptResult.Meta == "pid")
        {
            results.LastPid = int.Parse(scriptResult.Text);
        }
        else
        {
            results.Messages.AppendLine(scriptResult.Text);
        }
    }

    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        using var reader = new StreamReader(e.File.OpenReadStream());
        _content = await reader.ReadToEndAsync();

        _shell = Path.GetExtension(e.File.Name) switch
        {
            ".bat" => Shell.Cmd,
            ".cmd" => Shell.Cmd,
            ".ps1" => Shell.PowerShell,
            _ => _shell
        };
    }

    private void CleanResults()
    {
        _resultsPerComputer.Clear();
    }

    private async Task ExportResults()
    {
        var zipMemoryStream = new MemoryStream();

        using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var (computer, results) in _resultsPerComputer)
            {
                var fileName = $"results_{computer.Name}_{computer.IpAddress}.txt";
                var fileContent = results.ToString();

                var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                await using var entryStream = zipEntry.Open();
                await using var streamWriter = new StreamWriter(entryStream);
                await streamWriter.WriteAsync(fileContent);
            }
        }

        zipMemoryStream.Position = 0;
        var base64Zip = Convert.ToBase64String(zipMemoryStream.ToArray());

        await JsRuntime.InvokeVoidAsync("generateAndDownloadResults", base64Zip, "RemoteMaster_Results.zip");
    }
}
