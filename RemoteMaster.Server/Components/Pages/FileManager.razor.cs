// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class FileManager : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject(Key = "Resilience-Pipeline")]
    public ResiliencePipeline<string> ResiliencePipeline { get; set; } = default!;

    private HubConnection? _connection;
    private ClaimsPrincipal? _user;
    private string? _currentPath;
    private IBrowserFile? _selectedFile;
    private bool _firstRenderCompleted;
    private bool _disposed;

    private List<FileSystemItem> _items = [];
    private string? _selectedItem;

    private bool _isAccessDenied;

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
                await FetchAvailableDrives();
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

    private static void CreateNewItem()
    {

    }

    private static void DeleteSelectedItem()
    {

    }

    private void SelectItem(FileSystemItem item)
    {
        _selectedItem = item.Name;

        StateHasChanged();
    }

    private async Task HandleDoubleClick(FileSystemItem item)
    {
        if (item.Name == "..")
        {
            await NavigateUp();
        }
        else if (item.Type is FileSystemItemType.Drive or FileSystemItemType.Directory)
        {
            await ChangeDirectory(item.Name);
        }
    }

    private void OnInputFileChange(InputFileChangeEventArgs e)
    {
        _selectedFile = e.File;
    }

    private async Task UploadFile()
    {
        if (_selectedFile != null)
        {
            var data = new byte[_selectedFile.Size];
            var stream = _selectedFile.OpenReadStream(_selectedFile.Size);
            int bytesRead, totalBytesRead = 0;

            while (totalBytesRead < data.Length && (bytesRead = await stream.ReadAsync(data.AsMemory(totalBytesRead, data.Length - totalBytesRead))) > 0)
            {
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead != data.Length)
            {
                Logger.LogWarning("Fewer bytes were read than requested.");
            }

            var fileDto = new FileUploadDto(_selectedFile.Name, data, _currentPath);

            await SafeInvokeAsync(() => _connection!.InvokeAsync("UploadFile", fileDto));
        }
    }

    private async Task FetchAvailableDrives()
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetAvailableDrives"));
    }

    private async Task FetchFilesAndDirectories()
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetFilesAndDirectories", _currentPath));
    }

    private async Task InitializeHostConnectionAsync()
    {
        var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/filemanager", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                    return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                };
            })
            .AddMessagePackProtocol(options => options.Configure())
            .Build();

        _connection.On<List<FileSystemItem>>("ReceiveAvailableDrives", async drives =>
        {
            _items = drives;

            await InvokeAsync(StateHasChanged);
        });

        _connection.On<List<FileSystemItem>>("ReceiveFilesAndDirectories", async fileSystemItems =>
        {
            _items = fileSystemItems;

            await InvokeAsync(StateHasChanged);
        });

        _connection.On<byte[], string>("ReceiveFile", async (file, path) =>
        {
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

            var base64File = Convert.ToBase64String(file);
            var fileName = FileSystem.Path.GetFileName(path);

            const string contentType = "application/octet-stream;base64";

            await module.InvokeVoidAsync("downloadDataAsFile", base64File, fileName, contentType);
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

    private async Task DownloadFile(string fileName)
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("DownloadFile", $"{_currentPath}/{fileName}"));
    }

    private async Task ChangeDirectory(string directory)
    {
        _currentPath = string.IsNullOrEmpty(_currentPath) ? directory : FileSystem.Path.Combine(_currentPath, directory);

        await FetchFilesAndDirectories();
    }

    private async Task NavigateUp()
    {
        var parentDir = FileSystem.Directory.GetParent(_currentPath);

        if (parentDir != null)
        {
            _currentPath = parentDir.FullName;

            await FetchFilesAndDirectories();
        }
        else
        {
            _currentPath = null;

            await FetchAvailableDrives();
        }
    }

    private static string FormatSize(long size)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = size;
        var order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
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
}
