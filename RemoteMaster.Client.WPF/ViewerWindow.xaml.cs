using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.WPF;

public partial class ViewerWindow : MetroWindow
{
    private HubConnection? _agentConnection;

    public static HubConnection? ServerConnection { get; private set; }

    public ViewerWindow(string host)
    {
        InitializeComponent();

        InitializeConnectionAsync(host);
    }

    private async Task InitializeConnectionAsync(string host)
    {
        try
        {
            _agentConnection = new HubConnectionBuilder()
                .WithUrl($"http://{host}:3564/hubs/main", options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .Build();

            ServerConnection = new HubConnectionBuilder()
                .WithUrl($"http://{host}:5076/hubs/control", options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .AddMessagePackProtocol()
                .Build();

            RegisterServerHandlers();

            await _agentConnection.StartAsync();
            await ServerConnection.StartAsync();
        }
        catch (Exception)
        {
            // Handle exceptions as needed
        }
    }

    private void RegisterServerHandlers()
    {
        ServerConnection?.On<ScreenDataDto>("ScreenData", dto =>
        {
            var screenNumber = 1;

            foreach (var display in dto.Displays)
            {
                Dispatcher.Invoke(() =>
                {
                    var screenText = $"Screen {screenNumber++} ({display.Item3.Width} x {display.Item3.Height})";

                    var menuItem = new MenuItem
                    {
                        Header = screenText,
                        Tag = display.Item1
                    };

                    if (display.Item2)
                    {
                        menuItem.IsChecked = true;
                    }

                    menuItem.Click += OnDisplayMenuItemClick;

                    displays.Items.Add(menuItem);
                });
            }
        });

        ServerConnection?.On<ChunkWrapper>("ScreenUpdate", chunk =>
        {
            if (Chunker.TryUnchunkify(chunk, out var allData))
            {
                Task.Run(() =>
                {
                    BitmapSource bitmapSource;
                    using (var memory = new MemoryStream(allData))
                    {
                        memory.Position = 0;
                        var bitmap = new Bitmap(memory);
                        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                        bitmapSource = BitmapSource.Create(bitmap.Width, bitmap.Height, bitmap.HorizontalResolution, bitmap.VerticalResolution, PixelFormats.Bgra32, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                        bitmap.UnlockBits(bitmapData);
                        bitmapSource.Freeze();
                    }

                    Dispatcher.Invoke(() =>
                    {
                        screenImage.Source = bitmapSource;
                    });
                });
            }
        });
    }

    public static async Task TryInvokeServerAsync(string method)
    {
        if (IsConnectionReady(ServerConnection))
        {
            try
            {
                await ServerConnection?.InvokeAsync(method);
            }
            catch (Exception)
            {
                // Handle exception as needed
            }
        }
    }

    public static async Task TryInvokeServerAsync<T>(string method, T argument)
    {
        if (IsConnectionReady(ServerConnection))
        {
            try
            {
                await ServerConnection?.InvokeAsync(method, argument);
            }
            catch (Exception)
            {
                // Handle exception as needed
            }
        }
    }

    private (double, double) GetRelativeMousePositionOnPercent(MouseEventArgs e)
    {
        var currentPosition = e.GetPosition(screenImage);

        var percentX = currentPosition.X / screenImage.ActualWidth;
        var percentY = currentPosition.Y / screenImage.ActualHeight;

        return (percentX, percentY);
    }

    private async void OnMouseMove(object sender, MouseEventArgs e)
    {
        var xyPercent = GetRelativeMousePositionOnPercent(e);

        var dto = new MouseMoveDto
        {
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        };

        await TryInvokeServerAsync("SendMouseCoordinates", dto);
    }

    private static bool IsConnectionReady(HubConnection connection) => connection != null && connection.State == HubConnectionState.Connected;

    private async void OnKillServerClick(object sender, RoutedEventArgs e)
    {
        await TryInvokeServerAsync("KillServer");
    }

    private async void OnDisplayMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            await TryInvokeServerAsync("SendSelectedScreen", Convert.ToString(menuItem.Tag));
            menuItem.IsChecked = true;

            foreach (MenuItem item in displays.Items)
            {
                if (!item.Equals(menuItem))
                {
                    item.IsChecked = false;
                }
            }
        }
    }

    private async void screenImage_MouseUpDown(object sender, MouseButtonEventArgs e)
    {
        var xyPercent = GetRelativeMousePositionOnPercent(e);

        var dto = new MouseClickDto
        {
            Button = e.ChangedButton switch
            {
                MouseButton.Left => 0,
                MouseButton.Middle => 1,
                MouseButton.Right => 2,
            },
            State = e.ButtonState switch
            {
                MouseButtonState.Pressed => ButtonAction.Down,
                MouseButtonState.Released => ButtonAction.Up,
            },
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        };

        await TryInvokeServerAsync("SendMouseButton", dto);
    }

    private async void screenImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var dto = new MouseWheelDto
        {
            DeltaY = -e.Delta
        };

        await TryInvokeServerAsync("SendMouseWheel", dto);
    }

    private async void Window_KeyDownUp(object sender, KeyEventArgs e)
    {
        var dto = new KeyboardKeyDto
        {
            Key = KeyInterop.VirtualKeyFromKey(e.Key),
            State = e.IsDown ? ButtonAction.Down : ButtonAction.Up,
        };

        await TryInvokeServerAsync("SendKeyboardInput", dto);
    }

    private async void QualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var quality = (int)e.NewValue;

        await TryInvokeServerAsync("SetQuality", quality);
    }
}
