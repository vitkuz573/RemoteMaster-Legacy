// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.System.Ole;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class ClipboardService(ILogger<ClipboardService> logger) : IClipboardService
{
    public async Task<string?> GetTextAsync()
    {
        const int maxRetries = 3;
        const int delayMilliseconds = 100;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (!OpenClipboard(HWND.Null))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open clipboard.");
                }

                try
                {
                    using var handle = GetClipboardData_SafeHandle((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT);

                    return handle is { IsClosed: false, IsInvalid: false } ? GetClipboardText(handle) : null;
                }
                finally
                {
                    CloseClipboard();
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5 && attempt < maxRetries)
            {
                logger.LogWarning("Attempt {Attempt} to open clipboard failed. Retrying in {Delay}ms...", attempt, delayMilliseconds);

                await Task.Delay(delayMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving text from clipboard.");

                return null;
            }
        }

        logger.LogError("Failed to open clipboard after {MaxRetries} attempts.", maxRetries);

        return null;
    }

    public async Task SetTextAsync(string text)
    {
        await Task.Run(() =>
        {
            try
            {
                if (!OpenClipboard(HWND.Null))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open clipboard.");
                }

                try
                {
                    EmptyClipboard();

                    var bytes = (text.Length + 1) * 2;
                    var hGlobal = Marshal.AllocHGlobal(bytes);

                    try
                    {
                        Marshal.Copy(text.ToCharArray(), 0, hGlobal, text.Length);
                        Marshal.WriteInt16(hGlobal, text.Length * 2, 0);

                        if (SetClipboardData((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT, (HANDLE)hGlobal) == nint.Zero)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set clipboard data.");
                        }

                        hGlobal = nint.Zero;
                    }
                    finally
                    {
                        if (hGlobal != nint.Zero)
                        {
                            Marshal.FreeHGlobal(hGlobal);
                        }
                    }
                }
                finally
                {
                    CloseClipboard();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting text to clipboard.");
            }
        });
    }

    private static unsafe string? GetClipboardText(SafeHandle handle)
    {
        var pointer = GlobalLock(handle);

        if (pointer == null)
        {
            return null;
        }

        try
        {
            var length = 0;

            while (Marshal.ReadInt16((nint)pointer, length * 2) != 0)
            {
                length++;
            }

            var text = Marshal.PtrToStringUni((nint)pointer, length);

            return text;
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }
}
