// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Dialogs;

#pragma warning disable CA2227

public class CommonDialogBase : ComponentBase
{
    [CascadingParameter]
    protected MudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private IAccessTokenProvider AccessTokenProvider { get; set; } = default!;

    [Parameter]
    public ConcurrentDictionary<Computer, HubConnection?> Hosts { get; set; } = default!;

    [Parameter]
    public string ContentStyle { get; set; } = default!;

    [Parameter]
    public RenderFragment Content { get; set; } = default!;

    [Parameter]
    public RenderFragment Actions { get; set; } = default!;

    [Parameter]
    public string HubPath { get; set; } = "hubs/control";

    [Parameter]
    public bool StartConnection { get; set; } = true;

    [Parameter]
    public bool RequireConnections { get; set; } = true;

    private readonly ConcurrentDictionary<Computer, bool> _loadingStates = new();

    protected async void Cancel()
    {
        await FreeResources();
        MudDialog.Cancel();
    }

    public async Task FreeResources()
    {
        foreach (var connection in Hosts.Values.Where(connection => connection != null))
        {
            try
            {
                await connection.StopAsync();
                await connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
    }

    protected async override Task OnInitializedAsync()
    {
        if (RequireConnections)
        {
            await ConnectHosts();
        }
    }

    private async Task ConnectHosts()
    {
        var tasks = Hosts.Select(async kvp =>
        {
            var computer = kvp.Key;

            try
            {
                var connection = await SetupConnection(computer, HubPath, StartConnection, CancellationToken.None);
                Hosts[computer] = connection;
            }
            catch
            {
                Hosts[computer] = null;
            }
            finally
            {
                await InvokeAsync(StateHasChanged);
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task<HubConnection> SetupConnection(Computer computer, string hubPath, bool startConnection, CancellationToken cancellationToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{computer.IpAddress}:5001/{hubPath}", options =>
            {
                options.AccessTokenProvider = async () => await AccessTokenProvider.GetAccessTokenAsync();
            })
            .AddMessagePackProtocol()
            .Build();

        if (startConnection)
        {
            await connection.StartAsync(cancellationToken);
        }

        return connection;
    }

    public async Task RecheckConnection(Computer computer)
    {
        ArgumentNullException.ThrowIfNull(computer);

        _loadingStates[computer] = true;
        await InvokeAsync(StateHasChanged);

        if (Hosts.TryGetValue(computer, out var existingConnection) && existingConnection != null)
        {
            try
            {
                await existingConnection.StopAsync();
                await existingConnection.DisposeAsync();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        try
        {
            var newConnection = await SetupConnection(computer, HubPath, StartConnection, CancellationToken.None);
            Hosts[computer] = newConnection;
        }
        catch
        {
            Hosts[computer] = null;
        }
        finally
        {
            _loadingStates[computer] = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task RemoveComputer(Computer computer)
    {
        ArgumentNullException.ThrowIfNull(computer);

        if (Hosts.TryGetValue(computer, out var existingConnection) && existingConnection != null)
        {
            try
            {
                await existingConnection.StopAsync();
                await existingConnection.DisposeAsync();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        Hosts.TryRemove(computer, out _);
        await InvokeAsync(StateHasChanged);

        if (Hosts.IsEmpty)
        {
            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    public string GetRefreshIconClass(Computer computer)
    {
        return IsLoading(computer) ? "rotating" : string.Empty;
    }

    public string GetPanelHeaderText()
    {
        return RequireConnections && Hosts.Any(kvp => kvp.Value == null) ? "Click to view affected hosts (some hosts have issues)" : "Click to view affected hosts";
    }

    protected bool IsLoading(Computer computer) => _loadingStates.TryGetValue(computer, out var isLoading) && isLoading;
}
