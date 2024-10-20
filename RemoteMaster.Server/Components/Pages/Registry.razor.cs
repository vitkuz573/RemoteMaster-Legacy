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
    private readonly List<RegistryNode> _rootNodes = [];
    private string? _currentPath;
    private readonly List<RegistryValueDto> _registryValues = [];

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

    private async Task ToggleExpand(RegistryNode node)
    {
        node.IsExpanded = !node.IsExpanded;

        if (node.IsExpanded && node.SubKeys.Count == 0)
        {
            await LoadSubKeys(node.KeyFullPath);
        }
    }

    private async Task FetchRootKeys()
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetRootKeys"));
    }

    private async Task LoadSubKeys(string parentKey)
    {
        Logger.LogInformation("Loading subkeys for: {ParentKey}", parentKey);

        var hiveRoot = parentKey switch
        {
            string path when path.StartsWith(@"HKEY_LOCAL_MACHINE\") => "HKEY_LOCAL_MACHINE",
            string path when path.StartsWith(@"HKEY_CURRENT_USER\") => "HKEY_CURRENT_USER",
            string path when path.StartsWith(@"HKEY_CLASSES_ROOT\") => "HKEY_CLASSES_ROOT",
            string path when path.StartsWith(@"HKEY_USERS\") => "HKEY_USERS",
            string path when path.StartsWith(@"HKEY_CURRENT_CONFIG\") => "HKEY_CURRENT_CONFIG",
            _ => throw new InvalidOperationException("Unknown root key")
        };

        var keyPath = parentKey.Equals(hiveRoot, StringComparison.OrdinalIgnoreCase)
            ? null
            : parentKey[(hiveRoot.Length + 1)..].TrimStart('\\');

        Logger.LogInformation("Fetching subkeys for hive: {Hive}, keyPath: {KeyPath}", hiveRoot, keyPath ?? "<root>");

        var hive = hiveRoot switch
        {
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new InvalidOperationException("Unknown root key")
        };

        await _connection!.InvokeAsync("GetSubKeyNames", hive, keyPath, parentKey);
    }

    private RegistryNode? FindNodeByKey(string fullPath)
    {
        foreach (var rootNode in _rootNodes)
        {
            var foundNode = FindNodeRecursive(rootNode, fullPath);

            if (foundNode != null)
            {
                return foundNode;
            }
        }

        return null;
    }

    private static RegistryNode? FindNodeRecursive(RegistryNode currentNode, string fullPath)
    {
        if (currentNode.KeyFullPath.Equals(fullPath, System.StringComparison.OrdinalIgnoreCase))
        {
            return currentNode;
        }

        foreach (var subKey in currentNode.SubKeys)
        {
            var foundNode = FindNodeRecursive(subKey, fullPath);
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
                _rootNodes.Add(new RegistryNode
                {
                    KeyName = rootKey,
                    ParentKey = null,
                    SubKeys = []
                });
            }

            InvokeAsync(StateHasChanged);
        });

        _connection.On<IEnumerable<string>, string>("ReceiveSubKeyNames", async (subKeyNames, parentKey) =>
        {
            Logger.LogInformation("Received {SubKeyNamesCount} subkeys from server for key: {ParentKey}", subKeyNames.Count(), parentKey);

            var node = FindNodeByKey(parentKey);

            if (node != null)
            {
                Logger.LogInformation("Setting {SubKeyNamesCount} subkeys for node: {ParentKey}", subKeyNames.Count(), parentKey);

                node.SubKeys.Clear();

                foreach (var subKeyName in subKeyNames)
                {
                    node.SubKeys.Add(new RegistryNode
                    {
                        KeyName = subKeyName,
                        ParentKey = parentKey,
                        SubKeys = []
                    });
                }

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

    private class RegistryNode
    {
        public string KeyName { get; set; } = string.Empty;
        
        public bool IsExpanded { get; set; } = false;
        
        public List<RegistryNode> SubKeys { get; set; } = [];
        
        public string KeyFullPath => ParentKey == null ? KeyName : $"{ParentKey}\\{KeyName}";
        
        public string? ParentKey { get; set; }
    }
}
