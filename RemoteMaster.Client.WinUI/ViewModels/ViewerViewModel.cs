using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media.Imaging;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace RemoteMaster.Client.WinUI.ViewModels;

public partial class ViewerViewModel : ObservableRecipient, IDisposable
{
    [ObservableProperty]
    private BitmapImage _screenImage;

    private SynchronizationContext _uiContext;

    private HubConnection _serverConnection;

    public string Host
    {
        get;
        set;
    }

    public ViewerViewModel()
    {
        _uiContext = SynchronizationContext.Current;
    }

    public void InitializeServerConnection()
    {
        _serverConnection = new HubConnectionBuilder()
            .WithUrl($"http://{Host}:5076/hubs/control", options =>
            {
                options.SkipNegotiation = true;
                options.Transports = HttpTransportType.WebSockets;
            })
            .AddMessagePackProtocol()
            .Build();

        RegisterHandlers();

        StartServerConnection();
    }

    private void RegisterHandlers()
    {
        _serverConnection.On<ChunkWrapper>("ScreenUpdate", chunk =>
        {
            try
            {
                if (Chunker.TryUnchunkify(chunk, out var allData))
                {
                    _uiContext.Post(async _ =>
                    {
                        var image = new BitmapImage();

                        using (var stream = new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0, 1, 115, 14, 74, 86, 0, 0, 0, 9, 112, 72, 89, 115, 0, 0, 14, 196, 0, 0, 14, 196, 1, 149, 43, 14, 27, 0, 0, 0, 7, 116, 73, 77, 69, 7, 224, 2, 4, 15, 23, 0, 168, 219, 156, 101, 0, 0, 0, 19, 116, 69, 88, 116, 67, 111, 109, 109, 101, 110, 116, 0, 67, 114, 101, 97, 116, 101, 100, 32, 119, 105, 116, 104, 32, 71, 73, 77, 80, 87, 129, 14, 23, 0, 0, 0, 12, 73, 68, 65, 84, 8, 215, 99, 96, 96, 96, 0, 0, 0, 5, 0, 1, 114, 176, 166, 174, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 }))
                        {
                            await image.SetSourceAsync(stream.AsRandomAccessStream());
                        }

                        ScreenImage = image;
                    }, null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        });
    }

    private async void StartServerConnection()
    {
        try
        {
            await _serverConnection.StartAsync();
        }
        catch (Exception)
        {
            // Handle exception
        }
    }

    public async void CloseServerConnection()
    {
        if (_serverConnection != null)
        {
            await _serverConnection.DisposeAsync();
            _serverConnection = null;
        }
    }

    public void Dispose()
    {
        CloseServerConnection();
    }
}