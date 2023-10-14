using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host;

public unsafe class HiddenWindow
{
    private const string CLASS_NAME = "HiddenWindowClass";
    private readonly HWND _hwnd;
    private readonly WNDPROC _wndProcDelegate;

    private readonly ILogger<HiddenWindow> _logger;
    private readonly IClientService _clientService;

    public HiddenWindow(IClientService clientService, ILogger<HiddenWindow> logger)
    {
        _clientService = clientService;
        _logger = logger;

        _wndProcDelegate = WndProc;

        var wc = new WNDCLASSEXW();
        wc.cbSize = (uint)Marshal.SizeOf(wc);
        wc.lpfnWndProc = _wndProcDelegate;

        fixed (char* pClassName = CLASS_NAME)
        {
            wc.lpszClassName = pClassName;

            var atom = RegisterClassEx(in wc);

            if (atom == 0)
            {
                _logger.LogError("Failed to register the window class.");

                return;
            }
        }

        _hwnd = CreateWindowEx(0, CLASS_NAME, "", 0, 0, 0, 0, 0, HWND.HWND_MESSAGE, null, null, null);

        if (_hwnd.IsNull)
        {
            _logger.LogError("Failed to create hidden window.");

            return;
        }

        var result = WTSRegisterSessionNotification(_hwnd, NOTIFY_FOR_ALL_SESSIONS);

        if (!result)
        {
            _logger.LogError("Failed to register session notifications.");
        }
        else
        {
            _logger.LogInformation("Successfully registered for session notifications.");
        }
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_WTSSESSION_CHANGE)
        {
            var sessionChangeReason = (ulong)wParam.Value switch
            {
                WTS_CONSOLE_DISCONNECT => HandleSessionChange("A session was disconnected from the console terminal"),
                WTS_CONSOLE_CONNECT => HandleSessionChange("A session was connected to the console terminal"),
                _ => "Unknown session change reason."
            };

            _logger.LogInformation("Received session change notification. Reason: {SessionChangeReason}", sessionChangeReason);
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private string HandleSessionChange(string changeDescription)
    {
        RestartClient();

        return changeDescription;
    }

    public void RunMessageLoop()
    {
        MSG msg;

        while (GetMessage(out msg, _hwnd, 0, 0))
        {
            TranslateMessage(&msg);
            DispatchMessage(in msg);
        }
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
