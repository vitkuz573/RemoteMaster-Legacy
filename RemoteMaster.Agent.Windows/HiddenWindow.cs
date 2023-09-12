// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Abstractions;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Agent.Windows;

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
            var result = WTSRegisterSessionNotification((HWND)hwndSource.Handle, NOTIFY_FOR_ALL_SESSIONS);

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
            WTSUnRegisterSessionNotification((HWND)hwndSource.Handle);
            _logger.LogInformation("Unregistered from session notifications.");
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var source = PresentationSource.FromVisual(this) as HwndSource;
        source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_WTSSESSION_CHANGE)
        {
            var sessionChangeReason = wParam switch
            {
                (IntPtr)WTS_SESSION_LOCK => HandleSessionLock(lParam),
                (IntPtr)WTS_SESSION_UNLOCK => HandleSessionUnlock(lParam),
                (IntPtr)WTS_CONSOLE_DISCONNECT => HandleConsoleDisconnect(),
                (IntPtr)WTS_CONSOLE_CONNECT => HandleConsoleConnect(),
                _ => "Unknown session change reason."
            };

            _logger.LogInformation($"Received session change notification. Reason: {sessionChangeReason}");
        }

        return IntPtr.Zero;
    }

    private string HandleSessionLock(nint sessionId)
    {
        StopAndStartClient();
        return $"A session has been locked. Session ID: {sessionId}";
    }

    private string HandleSessionUnlock(nint sessionId)
    {
        StopAndStartClient();
        return $"A session has been unlocked. Session ID: {sessionId}";
    }

    private string HandleConsoleDisconnect()
    {
        StopAndStartClient();
        return "A session was disconnected from the console terminal.";
    }

    private string HandleConsoleConnect()
    {
        StopAndStartClient();
        return "A session was connected to the console terminal.";
    }

    private void StopAndStartClient()
    {
        _clientService.StopClient();
        while (_clientService.IsClientRunning())
        {
            Task.Delay(50).Wait();
        }
        _clientService.StartClient();
    }
}
