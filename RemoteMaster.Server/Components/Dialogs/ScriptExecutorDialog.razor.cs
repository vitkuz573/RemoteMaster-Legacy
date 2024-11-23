// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Compression;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ScriptExecutorDialog
{
    private string _content = string.Empty;
    private Shell _shell = Shell.Cmd;
    private bool _asSystem;
    private readonly Dictionary<HostDto, HostResults> _resultsPerHost = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private async Task RunScript()
    {
        foreach (var (host, connection) in Hosts)
        {
            if (connection != null && !_subscribedConnections.Contains(connection))
            {
                connection.On<Message>("ReceiveMessage", async message =>
                {
                    UpdateResultsForHost(host, message);
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
                await connection.InvokeAsync("ExecuteScript", scriptExecutionRequest);
            }
        }
    }

    private void UpdateResultsForHost(HostDto host, Message scriptResult)
    {
        if (!_resultsPerHost.TryGetValue(host, out var results))
        {
            results = new HostResults();
            _resultsPerHost[host] = results;
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

    private async Task UploadFiles(IBrowserFile? file)
    {
        if (file != null)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            _content = await reader.ReadToEndAsync();

            _shell = FileSystem.Path.GetExtension(file.Name) switch
            {
                ".bat" => Shell.Cmd,
                ".cmd" => Shell.Cmd,
                ".ps1" => Shell.PowerShell,
                _ => _shell
            };
        }
    }

    private void CleanResults()
    {
        _resultsPerHost.Clear();
    }

    private async Task ExportResults()
    {
        var zipMemoryStream = new MemoryStream();

        using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var (host, results) in _resultsPerHost)
            {
                var fileName = $"results_{host.Name}_{host.IpAddress}.txt";
                var fileContent = results.ToString();

                var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                await using var entryStream = zipEntry.Open();
                await using var streamWriter = new StreamWriter(entryStream);
                await streamWriter.WriteAsync(fileContent);
            }
        }

        zipMemoryStream.Position = 0;
        var base64Zip = Convert.ToBase64String(zipMemoryStream.ToArray());

        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

        await module.InvokeVoidAsync("downloadDataAsFile", base64Zip, "RemoteMaster_Results.zip", "application/zip;base64");
    }
}
