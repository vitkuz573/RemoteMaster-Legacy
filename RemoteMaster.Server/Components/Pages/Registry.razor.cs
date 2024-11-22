// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Microsoft.Win32;
using MudBlazor;
using Polly;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Extensions;

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
    private bool _isAccessDenied;

    private string? _selectedValue;

    private string? _currentPath;
    private readonly List<RegistryNode> _rootNodes = [];
    private readonly List<RegistryValueDto> _registryValues = [];

    private bool _firstRenderCompleted;
    private bool _disposed;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed && !_isAccessDenied)
        {
            _firstRenderCompleted = true;

            if (_isAccessDenied)
            {
                SnackBar.Add("Access denied. You do not have permission to access this host.", Severity.Error);
            }
            else
            {
                await InitializeHostConnectionAsync();
                await FetchRootKeys();
            }
        }
    }

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;

        _user = authState.User;

        var result = await HostAccessService.InitializeAccessAsync(Host, _user);

        _isAccessDenied = result.IsAccessDenied;

        if (_isAccessDenied && result.ErrorMessage != null)
        {
            SnackBar.Add(result.ErrorMessage, Severity.Error);
        }
    }

    private void SelectValue(RegistryValueDto registryValue)
    {
        _selectedValue = registryValue.Name;

        StateHasChanged();
    }

    private async Task ToggleExpand(RegistryNode node)
    {
        node.IsExpanded = !node.IsExpanded;

        if (node is { IsExpanded: true, SubKeys.Count: 0 })
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
            not null when parentKey.StartsWith("HKEY_LOCAL_MACHINE") => "HKEY_LOCAL_MACHINE",
            not null when parentKey.StartsWith("HKEY_CURRENT_USER") => "HKEY_CURRENT_USER",
            not null when parentKey.StartsWith("HKEY_CLASSES_ROOT") => "HKEY_CLASSES_ROOT",
            not null when parentKey.StartsWith("HKEY_USERS") => "HKEY_USERS",
            not null when parentKey.StartsWith("HKEY_CURRENT_CONFIG") => "HKEY_CURRENT_CONFIG",
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
        return _rootNodes.Select(rootNode => FindNodeRecursive(rootNode, fullPath)).OfType<RegistryNode>().FirstOrDefault();
    }

    private static RegistryNode? FindNodeRecursive(RegistryNode currentNode, string fullPath)
    {
        return currentNode.KeyFullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase) ? currentNode : currentNode.SubKeys.Select(subKey => FindNodeRecursive(subKey, fullPath)).OfType<RegistryNode>().FirstOrDefault();
    }

    private async Task FetchAllRegistryValues(RegistryHive hive, string keyPath)
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetAllRegistryValues", hive, keyPath));
    }

    private void SelectKey(string keyPath)
    {
        _currentPath = keyPath;

        _registryValues.Clear();

        var hiveRoot = keyPath switch
        {
            not null when keyPath.StartsWith("HKEY_LOCAL_MACHINE") => "HKEY_LOCAL_MACHINE",
            not null when keyPath.StartsWith("HKEY_CURRENT_USER") => "HKEY_CURRENT_USER",
            not null when keyPath.StartsWith("HKEY_CLASSES_ROOT") => "HKEY_CLASSES_ROOT",
            not null when keyPath.StartsWith("HKEY_USERS") => "HKEY_USERS",
            not null when keyPath.StartsWith("HKEY_CURRENT_CONFIG") => "HKEY_CURRENT_CONFIG",
            _ => throw new InvalidOperationException("Unknown root key")
        };

        var keyPathWithoutRoot = keyPath.Equals(hiveRoot, StringComparison.OrdinalIgnoreCase)
            ? null
            : keyPath[(hiveRoot.Length + 1)..].TrimStart('\\');

        var hive = hiveRoot switch
        {
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new InvalidOperationException("Unknown root key")
        };

        Logger.LogInformation("Fetching all registry values for hive: {Hive}, keyPath: {KeyPath}", hive, keyPathWithoutRoot ?? "<root>");

        _ = FetchAllRegistryValues(hive, keyPathWithoutRoot);
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
            .AddMessagePackProtocol(options => options.Configure())
            .Build();

        _connection.On<IEnumerable<string>>("ReceiveRootKeys", keys =>
        {
            _rootNodes.Clear();

            foreach (var rootKey in keys)
            {
                _rootNodes.Add(new RegistryNode(rootKey.TrimEnd('\\')));
            }

            InvokeAsync(StateHasChanged);
        });

        _connection.On<IEnumerable<string>, string>("ReceiveSubKeyNames", async (subKeyNames, parentKey) =>
        {
            var subKeyNameList = subKeyNames.ToList();

            Logger.LogInformation("Received {SubKeyNamesCount} subkeys from server for key: {ParentKey}", subKeyNameList.Count, parentKey);

            var node = FindNodeByKey(parentKey);

            if (node != null)
            {
                Logger.LogInformation("Setting {SubKeyNamesCount} subkeys for node: {ParentKey}", subKeyNameList.Count, parentKey);

                node.SubKeys.Clear();

                foreach (var subKeyName in subKeyNameList)
                {
                    node.SubKeys.Add(new RegistryNode(subKeyName, parentKey));
                }

                await InvokeAsync(StateHasChanged);
            }
            else
            {
                Logger.LogError("Node not found for: {ParentKey}", parentKey);
            }
        });

        _connection.On<IEnumerable<RegistryValueDto>>("ReceiveAllRegistryValues", async values =>
        {
            var valueList = values.ToList();

            Logger.LogInformation("Received {ValuesCount} registry values", valueList.Count);

            _registryValues.Clear();
            _registryValues.AddRange(valueList);

            await InvokeAsync(StateHasChanged);
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

    private RenderFragment RenderRegistryNode(RegistryNode node) => builder =>
    {
        var isSelected = _currentPath != null && _currentPath.Equals(node.KeyFullPath, StringComparison.OrdinalIgnoreCase);
        var selectedClass = isSelected ? "bg-blue-500 text-white" : "hover:bg-gray-200";

        builder.OpenElement(0, "li");

        builder.OpenElement(1, "div");
        builder.AddAttribute(2, "class", $"flex items-center space-x-2 cursor-pointer {selectedClass}");
        builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => SelectKey(node.KeyFullPath)));

        builder.OpenElement(4, "span");
        builder.AddAttribute(5, "class", "material-icons cursor-pointer");
        builder.AddEventStopPropagationAttribute(6, "onclick", true);
        builder.AddAttribute(7, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, async () => await ToggleExpand(node)));
        builder.AddContent(8, node.IsExpanded ? "expand_more" : "chevron_right");
        builder.CloseElement();

        builder.OpenElement(9, "span");
        builder.AddContent(10, node.KeyName);
        builder.CloseElement();

        builder.CloseElement();

        switch (node.IsExpanded)
        {
            case true when node.SubKeys.Count != 0:
            {
                builder.OpenElement(11, "ul");
                builder.AddAttribute(12, "class", "ml-4 whitespace-nowrap");

                foreach (var subNode in node.SubKeys)
                {
                    builder.AddContent(13, RenderRegistryNode(subNode));
                }

                builder.CloseElement();
                break;
            }
            case true when node.SubKeys.Count == 0:
                builder.OpenElement(14, "p");
                builder.AddContent(15, "No subkeys available");
                builder.CloseElement();
                break;
        }

        builder.CloseElement();
    };

    private RenderFragment RenderRegistryValue(RegistryValueDto registryValue) => builder =>
    {
        var isSelected = _selectedValue == registryValue.Name;
        var selectedClass = isSelected ? "bg-blue-100" : "";

        builder.OpenElement(0, "tr");
        builder.AddAttribute(1, "class", $"cursor-pointer border-t {selectedClass}");

        builder.AddEventPreventDefaultAttribute(2, "oncontextmenu", true);
        builder.AddAttribute(4, "onclick", EventCallback.Factory.Create(this, () => SelectValue(registryValue)));

        builder.OpenElement(5, "td");
        builder.AddAttribute(6, "class", "px-4 py-2 text-sm text-gray-700");
        builder.OpenElement(7, "input");
        builder.AddAttribute(8, "class", "w-full px-2 py-1");
        builder.AddAttribute(9, "value", registryValue.Name);

        builder.AddAttribute(10, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
        {
            registryValue.Name = e.Value.ToString();
        }));

        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(11, "td");
        builder.AddAttribute(12, "class", "px-4 py-2 text-sm text-gray-500");
        builder.OpenElement(13, "select");
        builder.AddAttribute(14, "class", "w-full px-2 py-1");
        builder.AddAttribute(15, "value", registryValue.ValueType.ToString());
        builder.AddAttribute(16, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
        {
            if (Enum.TryParse(typeof(RegistryValueKind), e.Value.ToString(), out var result))
            {
                registryValue.ValueType = (RegistryValueKind)result;
            }
        }));

        foreach (RegistryValueKind kind in Enum.GetValues(typeof(RegistryValueKind)))
        {
            builder.OpenElement(17, "option");
            builder.AddAttribute(18, "value", kind.ToString());
            builder.AddContent(19, kind.ToString());
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(20, "td");
        builder.AddAttribute(21, "class", "px-4 py-2 text-sm text-gray-500");

        switch (registryValue.ValueType)
        {
            case RegistryValueKind.Binary:
                var hexValue = BitConverter.ToString((byte[])registryValue.Value!).Replace("-", " ");
                builder.OpenElement(22, "input");
                builder.AddAttribute(23, "class", "w-full px-2 py-1");
                builder.AddAttribute(24, "value", hexValue);
                builder.AddAttribute(25, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                {
                    registryValue.Value = e.Value.ToString().Split(" ").Select(hex => Convert.ToByte(hex, 16)).ToArray();
                }));
                builder.CloseElement();
                break;

            case RegistryValueKind.String when IsRgbString(registryValue.Value.ToString()):
                var strValue = registryValue.Value.ToString();
                builder.OpenElement(26, "div");
                builder.AddAttribute(27, "class", "flex items-center");

                builder.OpenElement(28, "span");
                builder.AddAttribute(29, "class", "w-4 h-4 mr-2");
                builder.AddAttribute(30, "style", $"background-color: rgb({GetRgbColorFromString(strValue)});");
                builder.CloseElement();

                builder.OpenElement(31, "input");
                builder.AddAttribute(32, "class", "w-full px-2 py-1");
                builder.AddAttribute(33, "value", strValue);
                builder.AddAttribute(34, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                {
                    registryValue.Value = e.Value.ToString();
                }));
                builder.CloseElement();

                builder.CloseElement();
                break;

            default:
                builder.OpenElement(35, "input");
                builder.AddAttribute(36, "class", "w-full px-2 py-1");
                builder.AddAttribute(37, "value", registryValue.Value?.ToString());
                builder.AddAttribute(38, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                {
                    registryValue.Value = e.Value.ToString();
                }));
                builder.CloseElement();
                break;
        }

        builder.CloseElement();

        builder.CloseElement();
    };

    private RenderFragment RenderRegistryTable() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "flex-1 overflow-auto");

        builder.OpenElement(4, "table");
        builder.AddAttribute(5, "class", "min-w-full table-auto");

        builder.OpenElement(6, "thead");
        builder.AddAttribute(7, "class", "z-10 sticky top-0 bg-gray-100");

        builder.OpenElement(8, "tr");

        builder.OpenElement(9, "th");
        builder.AddAttribute(10, "class", "px-4 py-2 text-left text-xs font-semibold text-gray-500");
        builder.AddContent(11, "Name");
        builder.CloseElement();

        builder.OpenElement(12, "th");
        builder.AddAttribute(13, "class", "px-4 py-2 text-left text-xs font-semibold text-gray-500");
        builder.AddContent(14, "Type");
        builder.CloseElement();

        builder.OpenElement(15, "th");
        builder.AddAttribute(16, "class", "px-4 py-2 text-left text-xs font-semibold text-gray-500");
        builder.AddContent(17, "Value");
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(18, "tbody");
        builder.AddAttribute(19, "class", "bg-white");

        if (_registryValues.Any())
        {
            foreach (var registryValue in _registryValues)
            {
                builder.AddContent(20, RenderRegistryValue(registryValue));
            }
        }
        else
        {
            builder.OpenElement(21, "tr");
            builder.OpenElement(22, "td");
            builder.AddAttribute(23, "colspan", 3);
            builder.AddAttribute(24, "class", "px-4 py-2 text-center text-sm text-gray-400");
            builder.AddContent(25, "No values to display");
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();

        builder.CloseElement();
    };

    private void AddRegistryValue()
    {
        var newRegistryValue = new RegistryValueDto
        {
            Name = "New Value",
            ValueType = RegistryValueKind.String,
            Value = string.Empty
        };

        _registryValues.Add(newRegistryValue);
        _selectedValue = newRegistryValue.Name;

        StateHasChanged();
    }

    private void RemoveSelectedValue()
    {
        if (_selectedValue == null)
        {
            return;
        }

        var valueToRemove = _registryValues.FirstOrDefault(v => v.Name == _selectedValue);

        if (valueToRemove == null)
        {
            return;
        }

        _registryValues.Remove(valueToRemove);
        _selectedValue = null;

        StateHasChanged();
    }

    private static bool IsRgbString(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return false;
        }

        var parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        return parts.Length == 3 && parts.All(p => byte.TryParse(p, out _));
    }

    private static string GetRgbColorFromString(string str)
    {
        return str.Replace(" ", ",");
    }

    private async Task ExportRegistryBranch()
    {
        if (_currentPath == null)
        {
            return;
        }

        var hiveRoot = _currentPath switch
        {
            _ when _currentPath.StartsWith("HKEY_LOCAL_MACHINE") => "HKEY_LOCAL_MACHINE",
            _ when _currentPath.StartsWith("HKEY_CURRENT_USER") => "HKEY_CURRENT_USER",
            _ when _currentPath.StartsWith("HKEY_CLASSES_ROOT") => "HKEY_CLASSES_ROOT",
            _ when _currentPath.StartsWith("HKEY_USERS") => "HKEY_USERS",
            _ when _currentPath.StartsWith("HKEY_CURRENT_CONFIG") => "HKEY_CURRENT_CONFIG",
            _ => throw new InvalidOperationException("Unknown registry hive")
        };

        var keyPathWithoutHive = _currentPath.Equals(hiveRoot, StringComparison.OrdinalIgnoreCase)
            ? null
            : _currentPath[(hiveRoot.Length + 1)..].TrimStart('\\');

        var hive = hiveRoot switch
        {
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new InvalidOperationException("Unknown registry hive")
        };

        var exportResult = await _connection!.InvokeAsync<byte[]>("ExportRegistryBranch", hive, keyPathWithoutHive);
        var exportResultBase64 = Convert.ToBase64String(exportResult);
        var fileName = $"{_currentPath.Replace("\\", "_")}.reg";

        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");
        await module.InvokeVoidAsync("downloadDataAsFile", exportResultBase64, fileName, "application/octet-stream;base64");
    }

    [JSInvokable]
    public async Task OnBeforeUnload()
    {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_connection != null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while asynchronously disposing the connection for host {Host}", Host);
            }
        }

        _disposed = true;

        GC.SuppressFinalize(this);
    }

    private class RegistryNode(string keyName, string? parentKey = null)
    {
        public string KeyName { get; } = keyName;
        
        public bool IsExpanded { get; set; }
        
        public List<RegistryNode> SubKeys { get; } = [];

        public string KeyFullPath => ParentKey == null ? KeyName : $"{ParentKey}\\{KeyName}".TrimEnd('\\');

        private string? ParentKey { get; } = parentKey;
    }
}
