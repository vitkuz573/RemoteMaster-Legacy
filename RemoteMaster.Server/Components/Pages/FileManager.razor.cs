// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Polly;
using Polly.Retry;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class FileManager : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private HubConnection? _connection;
    private ClaimsPrincipal? _user;
    private string _searchQuery = string.Empty;
    private string _currentPath = string.Empty;
    private List<FileSystemItem> _fileSystemItems = [];
    private List<FileSystemItem> _allFileSystemItems = [];
    private List<string> _availableDrives = [];
    private string _selectedDrive = string.Empty;
    private IBrowserFile? _selectedFile;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
        [
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        ]);

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        _user = authState.User;

        if (_user?.Identity?.IsAuthenticated == true)
        {
            await InitializeHostConnectionAsync();
            await FetchAvailableDrives();

            if (_availableDrives.Count > 0)
            {
                _selectedDrive = _availableDrives.First();
                _currentPath = _selectedDrive;
                await FetchFilesAndDirectories();
            }
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
            await _selectedFile.OpenReadStream(_selectedFile.Size).ReadAsync(data);

            var fileDto = new FileUploadDto
            {
                Name = _selectedFile.Name,
                Data = data,
                DestinationPath = _currentPath
            };

            await SafeInvokeAsync(() => _connection!.InvokeAsync("UploadFile", fileDto));
        }
    }

    private async Task FetchAvailableDrives()
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetAvailableDrives"));
    }

    private async Task NavigateToPath()
    {
        await FetchFilesAndDirectories();
    }

    private async Task FetchFilesAndDirectories()
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("GetFilesAndDirectories", _currentPath));
    }

    private async Task InitializeHostConnectionAsync()
    {
        var userId = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _connection = new HubConnectionBuilder()
                .WithUrl($"https://{Host}:5001/hubs/filemanager", options =>
                {
                    options.AccessTokenProvider = async () => await AccessTokenProvider.GetAccessTokenAsync(userId);
                })
                .AddMessagePackProtocol()
                .Build();

            _connection.On<List<FileSystemItem>>("ReceiveFilesAndDirectories", async (fileSystemItems) =>
            {
                _fileSystemItems = fileSystemItems ?? [];
                _allFileSystemItems = new List<FileSystemItem>(_fileSystemItems); // Copy original list
                await InvokeAsync(StateHasChanged);
            });

            _connection.On<byte[], string>("ReceiveFile", async (file, path) =>
            {
                var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/fileUtils.js");

                var base64File = Convert.ToBase64String(file);
                var fileName = Path.GetFileName(path);
                var contentType = "application/octet-stream;base64";

                await module.InvokeVoidAsync("downloadDataAsFile", base64File, fileName, contentType);
            });

            _connection.On<List<string>>("ReceiveAvailableDrives", (drives) =>
            {
                _availableDrives = drives ?? [];
            });

            _connection.Closed += async (_) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                await _connection.StartAsync();
            };

            await _connection.StartAsync();
        }
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await action();
                }
                catch (HubException ex) when (ex.Message.Contains("Method does not exist"))
                {
                    await JsRuntime.InvokeVoidAsync("alert", "This function is not available in the current host version. Please update your host.");
                }
            }
            else
            {
                throw new InvalidOperationException("Connection is not active");
            }
        });
    }

    private async Task DownloadFile(string fileName)
    {
        await SafeInvokeAsync(() => _connection!.InvokeAsync("DownloadFile", $"{_currentPath}/{fileName}"));
    }

    private async Task ChangeDirectory(string directory)
    {
        _currentPath = Path.Combine(_currentPath, directory);
        await FetchFilesAndDirectories();
    }

    private async Task NavigateUp()
    {
        var parentDir = Directory.GetParent(_currentPath);

        if (parentDir != null)
        {
            _currentPath = parentDir.FullName;
            await FetchFilesAndDirectories();
        }
    }

    private async Task HandleClick(FileSystemItem item)
    {
        if (item.Name == "..")
        {
            await NavigateUp();
        }
        else if (item.Type == FileSystemItem.FileSystemItemType.Directory)
        {
            await ChangeDirectory(item.Name);
        }
        else
        {
            await DownloadFile(item.Name);
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

    private async Task UpdateSearchQuery(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? string.Empty;

        FilterItems();

        await InvokeAsync(StateHasChanged);
    }

    private void FilterItems()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            _fileSystemItems = new List<FileSystemItem>(_allFileSystemItems);
        }
        else
        {
            _fileSystemItems = _allFileSystemItems
                .Where(p => p.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private async Task OnDriveSelected(ChangeEventArgs e)
    {
        _selectedDrive = e.Value?.ToString() ?? string.Empty;
        _currentPath = _selectedDrive;

        await FetchFilesAndDirectories();
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
                Console.Error.WriteLine($"An error occurred while asynchronously disposing the connection for host {Host}: {ex.Message}");
            }
        }

        GC.SuppressFinalize(this);
    }
}