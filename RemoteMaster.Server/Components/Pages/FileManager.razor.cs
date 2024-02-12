// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Retry;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Pages;

public partial class FileManager : IDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    private HubConnection _connection = null!;
    private string _searchQuery = string.Empty;
    private string _currentPath = string.Empty;
    private List<FileSystemItem> _fileSystemItems = [];
    private List<string> _availableDrives = [];
    private string _selectedDrive = string.Empty;
    private IBrowserFile? _selectedFile;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        });

    private bool _isDarkMode = true;

    private readonly MudTheme _theme = new();

    protected async override Task OnInitializedAsync()
    {
        await InitializeHostConnectionAsync();
        await FetchAvailableDrives();

        if (_availableDrives.Count != 0)
        {
            _selectedDrive = _availableDrives.First();
            _currentPath = _selectedDrive;
            await FetchFilesAndDirectories();
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

            await _connection.InvokeAsync("UploadFile", fileDto);
        }
    }

    private async Task FetchAvailableDrives()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("GetAvailableDrives"));
    }

    private async Task NavigateToPath()
    {
        await FetchFilesAndDirectories();
    }

    private async Task FetchFilesAndDirectories()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("GetFilesAndDirectories", _currentPath));
    }

    private async Task InitializeHostConnectionAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        var accessToken = httpContext.Request.Cookies["accessToken"];

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Access token is not available.");
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/control", options =>
            {
                options.Headers.Add("Authorization", $"Bearer {accessToken}");
            })
            .AddMessagePackProtocol()
        .Build();

        _connection.On<List<FileSystemItem>>("ReceiveFilesAndDirectories", async (fileSystemItems) =>
        {
            _fileSystemItems = fileSystemItems;
            await InvokeAsync(StateHasChanged);
        });

        _connection.On<byte[], string>("ReceiveFile", async (file, path) =>
        {
            await JsRuntime.InvokeVoidAsync("saveAsFile", Path.GetFileName(path), Convert.ToBase64String(file));
        });

        _connection.On<List<string>>("ReceiveAvailableDrives", (drives) =>
        {
            _availableDrives = drives;
        });

        _connection.Closed += async (_) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
        };

        await _connection.StartAsync();
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await action();
                }
                catch (HubException ex) when (ex.Message.Contains("Method does not exist"))
                {
                    await JsRuntime.InvokeVoidAsync("showAlert", "This function is not available in the current host version. Please update your host.");
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
        await SafeInvokeAsync(() => _connection.InvokeAsync("DownloadFile", $"{_currentPath}/{fileName}"));
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

    private static string GetIcon(FileSystemItem.FileSystemItemType type)
    {
        return type == FileSystemItem.FileSystemItemType.Directory ? Icons.Material.Filled.Folder : Icons.Material.Filled.InsertDriveFile;
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

    private async Task OnDriveSelected(string selectedDrive)
    {
        _selectedDrive = selectedDrive;
        _currentPath = _selectedDrive;
        await FetchFilesAndDirectories();
    }

    private bool FilterFunc1(FileSystemItem fileSystemItem) => FilterFunc(fileSystemItem, _searchQuery);

    private static bool FilterFunc(FileSystemItem fileSystemItem, string searchQuery)
    {
        return string.IsNullOrWhiteSpace(searchQuery) || fileSystemItem.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
    }

    [JSInvokable]
    public void OnBeforeUnload()
    {
        Dispose();
    }

    public void Dispose()
    {
        _connection.DisposeAsync();
    }
}
