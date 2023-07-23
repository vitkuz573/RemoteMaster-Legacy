using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RemoteMaster.Client.WPF;

public partial class ViewerWindow : Window
{
    private readonly HubConnection? _agentConnection;

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
            ServerConnection = new HubConnectionBuilder()
                .WithUrl($"http://{host}:5076/hubs/control", options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .AddMessagePackProtocol()
                .Build();

            RegisterServerHandlers();

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

                    var comboBoxItem = new ComboBoxItem
                    {
                        Content = screenText,
                        Tag = display.Item1
                    };

                    if (display.Item2)
                    {
                        comboBoxItem.IsSelected = true;
                    }

                    displays.Items.Add(comboBoxItem);
                });
            }
        });

        ServerConnection?.On<ChunkWrapper>("ScreenUpdate", chunk =>
        {
            if (Chunker.TryUnchunkify(chunk, out var allData))
            {
                Dispatcher.Invoke(() =>
                {
                    var bitmapImage = new BitmapImage();

                    using (var memory = new MemoryStream(allData))
                    {
                        memory.Position = 0;
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                    }

                    screenImage.Source = bitmapImage;
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

    private async void QualityTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && int.TryParse(textBox.Text, out int quality))
        {
            await TryInvokeServerAsync("SetQuality", quality);
        }
    }

    private void OnNewMessageBoxClick(object sender, RoutedEventArgs e)
    {
        var messageBoxWindow = new MessageBoxWindow();
        messageBoxWindow.ShowDialog();
    }

    private async void OnKillServerClick(object sender, RoutedEventArgs e)
    {
        await TryInvokeServerAsync("KillServer");
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (drawerHost.IsRightDrawerOpen)
        {
            drawerHost.IsRightDrawerOpen = false;
            toggleButton.Content = "<";
        }
        else
        {
            drawerHost.IsRightDrawerOpen = true;
            toggleButton.Content = ">";
        }
    }

    private async void OnDisplaySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            var selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            await TryInvokeServerAsync("SendSelectedScreen", Convert.ToString(selectedItem.Tag));
            displays.IsDropDownOpen = false;
        }
    }
}
