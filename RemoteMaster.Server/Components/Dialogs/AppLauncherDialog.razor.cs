// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class AppLauncherDialog
{
    private bool _isShowPassword;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    private string _applicationPath = string.Empty;
    private string _destinationPath = string.Empty;
    private string _parameters = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;

    private readonly Dictionary<HostDto, HostResults> _resultsPerHost = [];
    private readonly HashSet<HubConnection> _subscribedConnections = [];

    private async Task Launch()
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

            var scriptBuilder = new StringBuilder();

            if (_applicationPath.StartsWith(@"\\"))
            {
                if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                {
                    scriptBuilder.AppendLine($"net use \"{_applicationPath}\" /user:{_username} {_password}");
                }

                if (!string.IsNullOrEmpty(_destinationPath))
                {
                    scriptBuilder.AppendLine($"copy \"{_applicationPath}\" \"{_destinationPath}\"");
                }
                else
                {
                    scriptBuilder.AppendLine($"\"{_applicationPath}\" {_parameters}");
                }

                scriptBuilder.AppendLine($"net use \"{_applicationPath}\" /delete");
            }
            else
            {
                scriptBuilder.AppendLine($"\"{_applicationPath}\" {_parameters}");
            }

            var scriptExecutionRequest = new ScriptExecutionRequest(scriptBuilder.ToString(), Shell.Cmd);

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

    private void TogglePasswordVisibility()
    {
        if (_isShowPassword)
        {
            _isShowPassword = false;
            _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
            _passwordInput = InputType.Password;
        }
        else
        {
            _isShowPassword = true;
            _passwordInputIcon = Icons.Material.Filled.Visibility;
            _passwordInput = InputType.Text;
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
