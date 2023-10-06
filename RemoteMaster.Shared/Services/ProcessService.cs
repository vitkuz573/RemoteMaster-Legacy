// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Native.Windows;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Services;

public class ProcessService : IProcessService
{
    public NativeProcess Start(ProcessStartOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var procInfo = new PROCESS_INFORMATION();
        var sessionId = GetSessionId(options.ForceConsoleSession, options.TargetSessionId);

        SafeFileHandle hUserTokenDup = null;
        SafeFileHandle stdOutputReadHandle = null;

        try
        {
            if (options.UseCurrentUserToken && TryGetUserToken(sessionId, out hUserTokenDup))
            {
                if (TryCreateInteractiveProcess(hUserTokenDup, options.ApplicationName, options.DesktopName, options.HiddenWindow, out procInfo, out stdOutputReadHandle))
                {
                    var resultProcess = new NativeProcess(procInfo, stdOutputReadHandle);
                    stdOutputReadHandle = null;

                    return resultProcess;
                }
            }
            else
            {
                var winlogonPid = GetWinlogonPidForSession(sessionId);
                using var hProcess = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, winlogonPid);

                if (IsProcessOpen(hProcess) && TryGetProcessToken(hProcess, out var hPToken))
                {
                    using (hPToken)
                    {
                        if (TryDuplicateToken(hPToken, out hUserTokenDup))
                        {
                            if (TryCreateInteractiveProcess(hUserTokenDup, options.ApplicationName, options.DesktopName, options.HiddenWindow, out procInfo, out stdOutputReadHandle))
                            {
                                var resultProcess = new NativeProcess(procInfo, stdOutputReadHandle);
                                stdOutputReadHandle = null;
                                
                                return resultProcess;
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            hUserTokenDup?.Dispose();
            stdOutputReadHandle?.Dispose();
        }

        return null;
    }

    private static bool IsProcessOpen(SafeHandle hProcess) => !hProcess.IsInvalid && !hProcess.IsClosed;

    private static bool TryGetProcessToken(SafeHandle hProcess, out SafeFileHandle hPToken) => OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out hPToken);

    private static bool TryDuplicateToken(SafeHandle hPToken, out SafeFileHandle hUserTokenDup) => DuplicateTokenEx(hPToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup);

    private static uint GetSessionId(bool forceConsoleSession, int? targetSessionId) => !forceConsoleSession ? FindTargetSessionId(targetSessionId.Value) : WTSGetActiveConsoleSessionId();

    private static uint GetWinlogonPidForSession(uint sessionId)
    {
        foreach (var process in Process.GetProcessesByName("winlogon"))
        {
            if ((uint)process.SessionId == sessionId)
            {
                return (uint)process.Id;
            }
        }

        throw new Exception("No winlogon process found for the given session id.");
    }

    private static uint FindTargetSessionId(int targetSessionId)
    {
        var activeSessions = SessionHelper.GetActiveSessions();
        uint lastSessionId = 0;
        var targetSessionFound = false;

        foreach (var session in activeSessions)
        {
            lastSessionId = session.Id;

            if (session.Id == targetSessionId)
            {
                targetSessionFound = true;
                break;
            }
        }

        return targetSessionFound ? (uint)targetSessionId : lastSessionId;
    }

    private static unsafe bool TryCreateInteractiveProcess(SafeHandle hUserTokenDup, string applicationName, string desktopName, bool hiddenWindow, out PROCESS_INFORMATION procInfo, out SafeFileHandle stdOutputReadHandle)
    {
        if (!CreatePipe(out stdOutputReadHandle, out SafeFileHandle stdOutputWriteHandle, null, 0))
        {
            throw new Exception("Failed to create pipe for standard output.");
        }

        fixed (char* pDesktopName = $@"winsta0\{desktopName}")
        {
            var startupInfo = new STARTUPINFOW
            {
                cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                lpDesktop = pDesktopName,
                hStdOutput = (HANDLE)stdOutputWriteHandle.DangerousGetHandle(),
                hStdError = (HANDLE)stdOutputWriteHandle.DangerousGetHandle(),
                dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES
            };

            var dwCreationFlags = SetCreationFlags(hiddenWindow);
            applicationName += char.MinValue;
            var appName = new Span<char>(applicationName.ToCharArray());

            bool result = CreateProcessAsUser(hUserTokenDup, null, ref appName, null, null, false, dwCreationFlags, null, null, startupInfo, out procInfo);
            stdOutputWriteHandle.Close();

            return result;
        }
    }

    private static PROCESS_CREATION_FLAGS SetCreationFlags(bool hiddenWindow)
    {
        var dwCreationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;
        dwCreationFlags |= hiddenWindow ? PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW : PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
        return dwCreationFlags;
    }

    private static bool TryGetUserToken(uint sessionId, out SafeFileHandle hUserToken)
    {
        var userTokenHandle = default(HANDLE);
        var success = WTSQueryUserToken(sessionId, ref userTokenHandle);
        hUserToken = new SafeFileHandle(userTokenHandle, true);
        return success;
    }
}