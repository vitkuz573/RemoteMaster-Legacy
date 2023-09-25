// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Core.Abstractions;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Agent;

public class HiddenWindow : Window
{
    private readonly ILogger<HiddenWindow> _logger;
    private readonly IClientService _clientService;

    public HiddenWindow()
    {
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<HiddenWindow>>();
        _clientService = serviceProvider.GetRequiredService<IClientService>();

        Visibility = Visibility.Hidden;
        ShowInTaskbar = false;
        WindowStyle = WindowStyle.None;

        Loaded += (sender, e) =>
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            var result = WTSRegisterSessionNotification((HWND)hwndSource!.Handle, NOTIFY_FOR_ALL_SESSIONS);

            if (!result)
            {
                _logger.LogError("Failed to register session notifications.");
            }
            else
            {
                _logger.LogInformation("Successfully registered for session notifications.");
            }
        };

        Unloaded += (sender, e) =>
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            WTSUnRegisterSessionNotification((HWND)hwndSource!.Handle);
            _logger.LogInformation("Unregistered from session notifications.");
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        
        var source = PresentationSource.FromVisual(this) as HwndSource;
        source!.AddHook(WndProc);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_WTSSESSION_CHANGE)
        {
            var sessionChangeReason = wParam switch
            {
                (nint)WTS_CONSOLE_DISCONNECT => HandleSessionChange("A session was disconnected from the console terminal"),
                (nint)WTS_CONSOLE_CONNECT => HandleSessionChange("A session was connected to the console terminal"),
                _ => "Unknown session change reason."
            };

            _logger.LogInformation("Received session change notification. Reason: {SessionChangeReason}", sessionChangeReason);
        }

        return nint.Zero;
    }

    private string HandleSessionChange(string changeDescription)
    {
        RestartClient();

        return changeDescription;
    }

    private void RestartClient()
    {
        _clientService.Stop();

        while (_clientService.IsRunning())
        {
            Task.Delay(50).Wait();
        }

        _clientService.Start();
    }
}
