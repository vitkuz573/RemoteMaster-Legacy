// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Host.Windows.Models;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

#pragma warning disable CA2000

public class NativeProcess(NativeProcessStartInfo startInfo) : IDisposable
{
    private SafeFileHandle? _processHandle;
    private StreamReader? _standardOutput;
    private StreamReader? _standardError;
    private uint? _processId;

    public NativeProcessStartInfo StartInfo { get; } = startInfo;

    public uint? Id => _processId;

    public StreamReader? StandardOutput => _standardOutput;

    public StreamReader? StandardError => _standardError;

    public void Start()
    {
        var sessionId = !StartInfo.ForceConsoleSession ? FindTargetSessionId(StartInfo.TargetSessionId) : WTSGetActiveConsoleSessionId();

        SafeFileHandle? hUserTokenDup = null;

        if (!StartInfo.UseCurrentUserToken || !TryGetUserToken(sessionId, out hUserTokenDup))
        {
            var winlogonPid = GetWinlogonPidForSession(sessionId);
            using var hProcess = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, winlogonPid);

            if (!hProcess.IsInvalid && !hProcess.IsClosed && OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hPToken))
            {
                using (hPToken)
                {
                    DuplicateTokenEx(hPToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup);
                }
            }
        }

        if (hUserTokenDup != null)
        {
            TryCreateInteractiveProcess(StartInfo, hUserTokenDup);
        }
        else
        {
            throw new InvalidOperationException("Failed to get or duplicate user token.");
        }
    }

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
        var activeSessions = GetActiveSessions();
        uint lastSessionId = 0;
        var targetSessionFound = false;

        foreach (var session in activeSessions)
        {
            lastSessionId = session.SessionId;

            if (session.SessionId == targetSessionId)
            {
                targetSessionFound = true;
                break;
            }
        }

        return targetSessionFound ? (uint)targetSessionId : lastSessionId;
    }

    private unsafe bool TryCreateInteractiveProcess(NativeProcessStartInfo startInfo, SafeHandle hUserTokenDup)
    {
        STARTUPINFOW startupInfo = default;
        PROCESS_INFORMATION processInformation = default;
        SECURITY_ATTRIBUTES securityAttributes = default;

        startupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOW>();

        if (!CreatePipe(out var stdInReadHandle, out var stdInWriteHandle, null, 0))
        {            
            return false;
        }

        if (!CreatePipe(out var stdOutReadHandle, out var stdOutWriteHandle, null, 0))
        {            
            return false;
        }

        if (!CreatePipe(out var stdErrReadHandle, out var stdErrWriteHandle, null, 0))
        {            
            return false;
        }

        if (!SetHandleInformation(stdOutWriteHandle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {            
            return false;
        }

        if (!SetHandleInformation(stdErrWriteHandle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {            
            return false;
        }

        fixed (char* pDesktopName = $@"winsta0\{startInfo.DesktopName}")
        {
            startupInfo.lpDesktop = pDesktopName;
        }

        startupInfo.hStdInput = (HANDLE)stdInReadHandle.DangerousGetHandle();
        startupInfo.hStdOutput = (HANDLE)stdOutWriteHandle.DangerousGetHandle();
        startupInfo.hStdError = (HANDLE)stdErrWriteHandle.DangerousGetHandle();
        startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;

        var dwCreationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;

        dwCreationFlags |= startInfo.CreateNoWindow
            ? PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW
            : PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;

        var fullCommand = $"{startInfo.FileName} {startInfo.Arguments ?? string.Empty}";
        fullCommand += char.MinValue;

        var commandSpan = new Span<char>(fullCommand.ToCharArray());
        var result = CreateProcessAsUser(hUserTokenDup, null, ref commandSpan, securityAttributes, securityAttributes, startInfo.InheritHandles, dwCreationFlags, null, null, startupInfo, out processInformation);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var consoleEncoding = Encoding.GetEncoding((int)GetConsoleOutputCP());

        _standardOutput = new StreamReader(new FileStream(stdOutReadHandle, FileAccess.Read), consoleEncoding);
        _standardError = new StreamReader(new FileStream(stdErrReadHandle, FileAccess.Read), consoleEncoding);
        _processHandle = new SafeFileHandle(processInformation.hProcess, true);
        _processId = processInformation.dwProcessId;

        stdInWriteHandle.Close();
        stdOutWriteHandle.Close();
        stdErrWriteHandle.Close();

        return result;
    }

    private static bool TryGetUserToken(uint sessionId, out SafeFileHandle hUserToken)
    {        
        var userTokenHandle = default(HANDLE);
        var success = WTSQueryUserToken(sessionId, ref userTokenHandle);
        hUserToken = new SafeFileHandle(userTokenHandle, true);

        return success;
    }

    internal static unsafe List<WTS_SESSION_INFOW> GetActiveSessions()
    {
        var sessions = new List<WTS_SESSION_INFOW>();

        if (WTSEnumerateSessions(HANDLE.Null, 0, 1, out var ppSessionInfo, out var count))
        {
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var session = ppSessionInfo[i];

                    if (session.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                    {
                        sessions.Add(session);
                    }
                }
            }
            finally
            {
                WTSFreeMemory(ppSessionInfo);
            }
        }

        return sessions;
    }

    public bool WaitForExit(uint millisecondsTimeout = uint.MaxValue)
    {
        if (_processHandle == null || _processHandle.IsInvalid || _processHandle.IsClosed)
        {
            throw new InvalidOperationException("No process is associated with this NativeProcess object.");
        }

        var result = WaitForSingleObject(_processHandle, millisecondsTimeout);

        return result == WAIT_EVENT.WAIT_OBJECT_0;
    }

    public void Dispose()
    {
        _standardOutput?.Dispose();
        _standardError?.Dispose();
        _processHandle?.Close();
    }
}