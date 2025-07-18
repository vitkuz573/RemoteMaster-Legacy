﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Extensions;

namespace RemoteMaster.Server.Components.Dialogs;

public class CommonDialogBase : ComponentBase, IAsyncDisposable
{
    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private IAccessTokenProvider AccessTokenProvider { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private ISslWarningService SslWarningService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

#pragma warning disable CA2227
    [CascadingParameter]
    public ConcurrentDictionary<HostDto, HubConnection?> Hosts { get; set; } = default!;
#pragma warning restore CA2227

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

    private string? _accessToken;

    protected async Task CancelAsync()
    {
        await DisposeAsync();

        MudDialog.Cancel();
    }

    protected async override Task OnInitializedAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        var user = httpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");
        var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

        _accessToken = accessTokenResult.IsSuccess ? accessTokenResult.Value : null;

        if (RequireConnections)
        {
            await ConnectHosts();
        }
    }

    private async Task ConnectHosts()
    {
        var tasks = Hosts.Select(async kvp =>
        {
            var host = kvp.Key;
            _checkingStates[host] = true;

            try
            {
                var connection = await SetupConnection(host, HubPath, StartConnection, CancellationToken.None);
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

    private async Task<bool> ShowSslWarningDialog(IPAddress ipAddress, SslPolicyErrors sslPolicyErrors, CertificateInfo certificateInfo)
    {
        var parameters = new DialogParameters<SslWarningDialog>
        {
            { d => d.IpAddress, ipAddress },
            { d => d.SslPolicyErrors, sslPolicyErrors },
            { d => d.CertificateInfo, certificateInfo }
        };

        var dialog = await DialogService.ShowAsync<SslWarningDialog>("SSL Certificate Warning", parameters);
        var result = await dialog.Result;

        return !result?.Canceled ?? throw new InvalidOperationException("Result not found.");
    }

    private async Task<HubConnection> SetupConnection(HostDto host, string hubPath, bool startConnection, CancellationToken cancellationToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{host.IpAddress}:5001/{hubPath}", options =>
            {
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = (_, cert, chain, sslPolicyErrors) =>
                        {
                            if (SslWarningService.IsSslAllowed(host.IpAddress))
                            {
                                return true;
                            }

                            int keySize;

                            if (cert == null)
                            {
                                return false;
                            }

                            switch (cert.PublicKey.Oid.Value)
                            {
                                case "1.2.840.113549.1.1.1":
                                {
                                    using var rsa = cert.GetRSAPublicKey();
                                    keySize = rsa?.KeySize ?? 0;
                                    break;
                                }
                                case "1.2.840.10040.4.1":
                                {
                                    using var dsa = cert.GetDSAPublicKey();
                                    keySize = dsa?.KeySize ?? 0;
                                    break;
                                }
                                case "1.2.840.10045.2.1":
                                {
                                    using var ecdsa = cert.GetECDsaPublicKey();
                                    keySize = ecdsa?.KeySize ?? 0;
                                    break;
                                }
                                default:
                                {
                                    keySize = 0;
                                    break;
                                }
                            }

                            var certificateInfo = new CertificateInfo(
                                cert.Issuer,
                                cert.Subject,
                                cert.GetExpirationDateString(),
                                cert.GetEffectiveDateString(),
                                cert.SignatureAlgorithm.FriendlyName ?? "Unknown",
                                keySize.ToString(),
                                chain?.ChainElements.Select(e => e.Certificate.Subject).ToList() ?? []
                            );

                            return sslPolicyErrors == SslPolicyErrors.None || Task.Run(() => ShowSslWarningDialog(host.IpAddress, sslPolicyErrors, certificateInfo), cancellationToken).Result;

                        };
                    }

                    return handler;
                };

                options.AccessTokenProvider = async () => await Task.FromResult(_accessToken);
            })
            .AddMessagePackProtocol(options => options.Configure())
            .Build();

        if (startConnection)
        {
            await connection.StartAsync(cancellationToken);
        }

        return connection;
    }

    protected async Task RecheckConnectionAsync(HostDto host)
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
            var newConnection = await SetupConnection(host, HubPath, StartConnection, CancellationToken.None);
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

    protected async Task RemoveHostAsync(HostDto host)
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

    protected string GetPanelHeaderText()
    {
        return RequireConnections && Hosts.Any(kvp => kvp.Value == null) ? "Click to view affected hosts (some hosts have issues)" : "Click to view affected hosts";
    }

    protected string GetButtonClass(HostDto host)
    {
        const string baseClass = "fixed-size-button";

        return IsLoading(host) ? $"{baseClass} rotating" : baseClass;
    }

    protected bool IsRefreshDisabled(HostDto host)
    {
        return IsLoading(host) || IsChecking(host);
    }

    protected bool IsChecking(HostDto host) => _checkingStates.TryGetValue(host, out var isChecking) && isChecking;

    private bool IsLoading(HostDto host) => _loadingStates.TryGetValue(host, out var isLoading) && isLoading;

    protected string GetErrorMessage(HostDto host) => _errorMessages.TryGetValue(host, out var errorMessage) ? errorMessage : "Unknown error";

    public string GetHostStatus(HostDto host)
    {
        if (IsChecking(host))
        {
            return "Checking";
        }

        if (Hosts.TryGetValue(host, out var connection) && connection != null)
        {
            return "Connected";
        }

        return "Error";
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in Hosts.Values.Where(connection => connection != null))
        {
            try
            {
                await connection!.StopAsync();
                await connection.DisposeAsync();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
