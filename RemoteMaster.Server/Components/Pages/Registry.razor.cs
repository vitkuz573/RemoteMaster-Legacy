// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Microsoft.Win32;
using Polly;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Formatters;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class Registry : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject(Key = "Resilience-Pipeline")]
    public ResiliencePipeline<string> ResiliencePipeline { get; set; } = default!;

    private HubConnection? _connection;
    private ClaimsPrincipal? _user;

    private readonly List<string> _rootKeys = [];
    private string? _currentPath;
    private readonly List<RegistryValueDto> _registryValues = [];
    private readonly List<RegistryKeyNode> _rootNodes = [];

    private bool _firstRenderCompleted;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _firstRenderCompleted = true;
        }
    }

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        _user = authState.User;

        if (_user?.Identity?.IsAuthenticated == true)
        {
            await InitializeHostConnectionAsync();
            await FetchRootKeys();
        }
    }

    private async Task FetchRootKeys()
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetRootKeys"));
    }

    private async Task LoadSubKeys(string parentKey)
    {
        Logger.LogInformation("Loading subkeys for: {ParentKey}", parentKey);

        var hive = parentKey switch
        {
            string path when path.StartsWith(@"HKEY_LOCAL_MACHINE\") => RegistryHive.LocalMachine,
            string path when path.StartsWith(@"HKEY_CURRENT_USER\") => RegistryHive.CurrentUser,
            string path when path.StartsWith(@"HKEY_CLASSES_ROOT\") => RegistryHive.ClassesRoot,
            string path when path.StartsWith(@"HKEY_USERS\") => RegistryHive.Users,
            string path when path.StartsWith(@"HKEY_CURRENT_CONFIG\") => RegistryHive.CurrentConfig,
            _ => throw new InvalidOperationException("Unknown root key")
        };

        var keyPath = parentKey switch
        {
            @"HKEY_LOCAL_MACHINE\" => null,
            @"HKEY_CURRENT_USER\" => null,
            @"HKEY_CLASSES_ROOT\" => null,
            @"HKEY_USERS\" => null,
            @"HKEY_CURRENT_CONFIG\" => null,
            _ => parentKey[(hive.ToString().Length + 1)..]
        };

        await _connection!.InvokeAsync("GetSubKeyNames", hive, keyPath, parentKey);
    }

    private RegistryKeyNode? FindNodeByKey(string fullPath)
    {
        foreach (var rootNode in _rootNodes)
        {
            if (fullPath.Equals(rootNode.KeyName, StringComparison.OrdinalIgnoreCase))
            {
                return rootNode;
            }

            var foundNode = rootNode.FindNodeByKey(fullPath);

            if (foundNode != null)
            {
                return foundNode;
            }
        }
        return null;
    }

    private async Task FetchAllRegistryValues(RegistryHive hive, string keyPath)
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetAllRegistryValues", hive, keyPath));
    }

    private void SelectKey(string keyPath)
    {
        _currentPath = keyPath;

        _registryValues.Clear();

        var hive = keyPath switch
        {
            string path when path.StartsWith(@"HKEY_LOCAL_MACHINE\") => RegistryHive.LocalMachine,
            string path when path.StartsWith(@"HKEY_CURRENT_USER\") => RegistryHive.CurrentUser,
            string path when path.StartsWith(@"HKEY_CLASSES_ROOT\") => RegistryHive.ClassesRoot,
            string path when path.StartsWith(@"HKEY_USERS\") => RegistryHive.Users,
            string path when path.StartsWith(@"HKEY_CURRENT_CONFIG\") => RegistryHive.CurrentConfig,
            _ => throw new InvalidOperationException("Unknown root key")
        };

        _ = FetchAllRegistryValues(hive, keyPath[hive.ToString().Length..]);
    }

    private async Task InitializeHostConnectionAsync()
    {
        var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/registry", options =>
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

        _connection.On<IEnumerable<string>>("ReceiveRootKeys", keys =>
        {
            _rootKeys.Clear();
            _rootKeys.AddRange(keys);

            _rootNodes.Clear();

            foreach (var rootKey in _rootKeys)
            {
                _rootNodes.Add(new RegistryKeyNode
                {
                    KeyName = rootKey,
                    SubKeys = []
                });
            }
        });

        _connection.On<IEnumerable<string>, string>("ReceiveSubKeyNames", async (subKeyNames, parentKey) =>
        {
            Logger.LogInformation("Received {SubKeyNamesCount} subkeys from server for key: {ParentKey}", subKeyNames.Count(), parentKey);

            var node = FindNodeByKey(parentKey);

            if (node != null)
            {
                Logger.LogInformation("Setting {SubKeyNamesCount} subkeys for node: {ParentKey}", subKeyNames.Count(), parentKey);

                node.SetSubKeys(subKeyNames);

                await InvokeAsync(StateHasChanged);
            }
            else
            {
                Logger.LogError("Node not found for: {ParentKey}", parentKey);
            }
        });

        _connection.On<IEnumerable<RegistryValueDto>>("ReceiveAllRegistryValues", values =>
        {
            _registryValues.Clear();
            _registryValues.AddRange(values);
        });

        _connection.Closed += async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
        };

        await _connection.StartAsync();
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        var result = await ResiliencePipeline.ExecuteAsync(async _ =>
        {
            if (_connection is not { State: HubConnectionState.Connected })
            {
                throw new InvalidOperationException("Connection is not active");
            }

            await action();
            await Task.CompletedTask;

            return "Success";

        }, CancellationToken.None);

        if (result == "This function is not available in the current host version. Please update your host.")
        {
            if (_firstRenderCompleted)
            {
                await JsRuntime.InvokeVoidAsync("alert", result);
            }
        }
    }

    [JSInvokable]
    public async Task OnBeforeUnload()
    {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("An error occurred while asynchronously disposing the connection for host {Host}: {Message}", Host, ex.Message);
            }
        }

        GC.SuppressFinalize(this);
    }
}
