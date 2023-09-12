// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Agent.Windows;

public class HiddenWindow : Window
{
    private readonly ILogger<HiddenWindow> _logger;

    public HiddenWindow()
    {
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<HiddenWindow>>();

        _logger.LogInformation("Initializing HiddenWindow.");

        Visibility = Visibility.Hidden;
        ShowInTaskbar = false;
        WindowStyle = WindowStyle.None;

        Loaded += (sender, e) =>
        {
            _logger.LogInformation("HiddenWindow Loaded event triggered.");

            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            var result = WTSRegisterSessionNotification((HWND)hwndSource.Handle, NOTIFY_FOR_ALL_SESSIONS);

            if (!result)
            {
                _logger.LogError("Failed to register session notifications.");
                // Обработайте ошибку
            }
            else
            {
                _logger.LogInformation("Successfully registered for session notifications.");
            }
        };

        Unloaded += (sender, e) =>
        {
            _logger.LogInformation("HiddenWindow Unloaded event triggered.");

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

        _logger.LogInformation("Source initialized and hook added.");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_WTSSESSION_CHANGE)
        {
            var sessionChangeReason = wParam switch
            {
                (IntPtr)WTS_CONSOLE_CONNECT => "A session was connected to the console terminal.",
                (IntPtr)WTS_CONSOLE_DISCONNECT => "A session was disconnected from the console terminal.",
                (IntPtr)WTS_REMOTE_CONNECT => "A session was connected to the remote terminal.",
                (IntPtr)WTS_REMOTE_DISCONNECT => "A session was disconnected from the remote terminal.",
                (IntPtr)WTS_SESSION_LOGON => $"A user has logged on to the session. Session ID: {lParam}",
                (IntPtr)WTS_SESSION_LOGOFF => $"A user has logged off the session. Session ID: {lParam}",
                (IntPtr)WTS_SESSION_LOCK => $"A session has been locked. Session ID: {lParam}",
                (IntPtr)WTS_SESSION_UNLOCK => $"A session has been unlocked. Session ID: {lParam}",
                (IntPtr)WTS_SESSION_REMOTE_CONTROL => $"A session has changed its remote controlled status. Session ID: {lParam}",
                _ => "Unknown session change reason."
            };

            _logger.LogInformation($"Received session change notification. Reason: {sessionChangeReason}");
        }

        return IntPtr.Zero;
    }
}
