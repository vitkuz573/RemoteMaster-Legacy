// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Formatters;

namespace RemoteMaster.Server.Components.Dialogs;

#pragma warning disable

public class CommonDialogBase : ComponentBase
{
    [CascadingParameter]
    protected MudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private IAccessTokenProvider AccessTokenProvider { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [CascadingParameter]
    public ConcurrentDictionary<HostDto, HubConnection?> Hosts { get; set; } = default!;

    [Parameter]
    public string ContentStyle { get; set; } = default!;

    [Parameter]
    public RenderFragment Content { get; set; } = default!;

    [Parameter]
    public RenderFragment Actions { get; set; } = default!;

    [CascadingParameter]
    public string HubPath { get; set; } = default!;

    [CascadingParameter]
    public bool StartConnection { get; set; }

    [CascadingParameter]
    public bool RequireConnections { get; set; }

    public bool HasConnectionIssues => RequireConnections && Hosts.Any(kvp => kvp.Value == null);

    private readonly ConcurrentDictionary<HostDto, bool> _checkingStates = new();
    private readonly ConcurrentDictionary<HostDto, bool> _loadingStates = new();
    private readonly ConcurrentDictionary<HostDto, string> _errorMessages = new();

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
        var httpContext = HttpContextAccessor.HttpContext;

        var tasks = Hosts.Select(async kvp =>
        {
            var host = kvp.Key;
            _checkingStates[host] = true;

            try
            {
                var userId = UserManager.GetUserId(httpContext.User);
                var connection = await SetupConnection(userId, host, HubPath, StartConnection, CancellationToken.None);
                Hosts[host] = connection;
            }
            catch (Exception ex)
            {
                Hosts[host] = null;
                _errorMessages[host] = ex.Message;
            }
            finally
            {
                _checkingStates[host] = false;
                await InvokeAsync(StateHasChanged);
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task<HubConnection> SetupConnection(string userId, HostDto host, string hubPath, bool startConnection, CancellationToken cancellationToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{host.IpAddress}:5001/{hubPath}", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);
                    return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                };
            })
            .AddMessagePackProtocol(options =>
            {
                var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            })
            .Build();

        if (startConnection)
        {
            await connection.StartAsync(cancellationToken);
        }

        return connection;
    }

    public async Task RecheckConnection(HostDto host)
    {
        ArgumentNullException.ThrowIfNull(host);

        _loadingStates[host] = true;
        _checkingStates[host] = true;
        await InvokeAsync(StateHasChanged);

        if (Hosts.TryGetValue(host, out var existingConnection) && existingConnection != null)
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
            var httpContext = HttpContextAccessor.HttpContext;

            var userId = UserManager.GetUserId(httpContext.User);
            var newConnection = await SetupConnection(userId, host, HubPath, StartConnection, CancellationToken.None);
            Hosts[host] = newConnection;
            _errorMessages.TryRemove(host, out _);
        }
        catch (Exception ex)
        {
            Hosts[host] = null;
            _errorMessages[host] = ex.Message;
        }
        finally
        {
            _loadingStates[host] = false;
            _checkingStates[host] = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task RemoveHost(HostDto host)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (Hosts.TryGetValue(host, out var existingConnection) && existingConnection != null)
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

        Hosts.TryRemove(host, out _);
        _errorMessages.TryRemove(host, out _);
        await InvokeAsync(StateHasChanged);

        if (Hosts.IsEmpty)
        {
            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    public string GetRefreshIconClass(HostDto host)
    {
        return IsLoading(host) ? "rotating" : string.Empty;
    }

    public string GetPanelHeaderText()
    {
        return RequireConnections && Hosts.Any(kvp => kvp.Value == null) ? "Click to view affected hosts (some hosts have issues)" : "Click to view affected hosts";
    }

    public string GetButtonClass(HostDto host)
    {
        var baseClass = "fixed-size-button";

        if (IsLoading(host))
        {
            return $"{baseClass} rotating";
        }

        return baseClass;
    }

    public bool IsRefreshDisabled(HostDto host)
    {
        return IsLoading(host) || IsChecking(host);
    }

    protected bool IsChecking(HostDto host) => _checkingStates.TryGetValue(host, out var isChecking) && isChecking;

    protected bool IsLoading(HostDto host) => _loadingStates.TryGetValue(host, out var isLoading) && isLoading;

    public string GetErrorMessage(HostDto host) => _errorMessages.TryGetValue(host, out var errorMessage) ? errorMessage : "Unknown error";
}
