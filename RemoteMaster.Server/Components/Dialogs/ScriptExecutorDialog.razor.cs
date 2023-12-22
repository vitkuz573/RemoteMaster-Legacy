// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ScriptExecutorDialog
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private string _content;
    private Shell? _shell;
    private bool _asSystem;
    private readonly Dictionary<Computer, StringBuilder> _resultsPerComputer = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private async Task RunScript()
    {
        foreach (var (computer, connection) in Hosts)
        {
            if (connection != null && !_subscribedConnections.Contains(connection))
            {
                connection.On<ScriptResult>("ReceiveScriptResult", async scriptResult =>
                {
                    UpdateResultsForComputer(computer, scriptResult);
                    await InvokeAsync(StateHasChanged);
                });

                _subscribedConnections.Add(connection);
            }

            if (connection != null)
            {
                await connection.InvokeAsync("SendScript", _content, _shell, _asSystem);
            }
        }
    }

    private void UpdateResultsForComputer(Computer computer, ScriptResult scriptResult)
    {
        if (!_resultsPerComputer.TryGetValue(computer, out var stringBuilder))
        {
            stringBuilder = new StringBuilder();
            _resultsPerComputer[computer] = stringBuilder;
        }

        var messagePrefix = scriptResult.Type == ScriptResult.MessageType.Error ? "[Error] " : "[Output] ";
        stringBuilder.AppendLine(messagePrefix + scriptResult.Message);
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
                var fileName = $"results_{computer.Name}_{computer.IPAddress}.txt";
                var fileContent = results.ToString();

                var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                using var entryStream = zipEntry.Open();
                using var streamWriter = new StreamWriter(entryStream);
                await streamWriter.WriteAsync(fileContent);
            }
        }

        zipMemoryStream.Position = 0;
        var base64Zip = Convert.ToBase64String(zipMemoryStream.ToArray());

        await JSRuntime.InvokeVoidAsync("generateAndDownloadResults", base64Zip, "RemoteMaster_Results.zip");
    }
}
