// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Host.Windows.Models;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

#pragma warning disable CA2000

public class NativeProcess
{
    private SafeFileHandle? _processHandle;
    private StreamReader? _standardOutput;
    private StreamReader? _standardError;
    private uint? _processId;

    public NativeProcessStartInfo StartInfo { get; private set; }

    public uint? Id => _processId;

    public StreamReader? StandardOutput => _standardOutput;

    public StreamReader? StandardError => _standardError;

    public NativeProcess(NativeProcessStartInfo startInfo)
    {
        Log.Information("Initializing NativeProcess with StartInfo: {@StartInfo}", startInfo);
        StartInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
    }

    public void Start()
    {
        ArgumentNullException.ThrowIfNull(StartInfo);

        Log.Information("Starting NativeProcess with StartInfo: {@StartInfo}", StartInfo);
        var sessionId = !StartInfo.ForceConsoleSession ? FindTargetSessionId(StartInfo.TargetSessionId) : WTSGetActiveConsoleSessionId();
        Log.Debug("Session ID determined: {SessionId}", sessionId);

        SafeFileHandle? hUserTokenDup = null;

        try
        {
            if (!StartInfo.UseCurrentUserToken || !TryGetUserToken(sessionId, out hUserTokenDup))
            {
                var winlogonPid = GetWinlogonPidForSession(sessionId);
                Log.Debug("Winlogon PID for session {SessionId}: {WinlogonPid}", sessionId, winlogonPid);

                using var hProcess = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, winlogonPid);
                Log.Debug("Process handle opened: {ProcessHandle}", hProcess);

                if (!hProcess.IsInvalid && !hProcess.IsClosed && OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hPToken))
                {
                    using (hPToken)
                    {
                        DuplicateTokenEx(hPToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup);
                        Log.Debug("User token duplicated: {UserTokenDup}", hUserTokenDup);
                    }
                }
                else
                {
                    Log.Error("Failed to open or duplicate user token for session {SessionId}", sessionId);
                }
            }
            
            if (hUserTokenDup != null)
            {
                if (!TryCreateInteractiveProcess(StartInfo, hUserTokenDup))
                {
                    Log.Error("Failed to create interactive process.");
                }
            }
            else
            {
                Log.Error("User token duplication failed.");
                throw new InvalidOperationException("Failed to get or duplicate user token.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while starting NativeProcess.");
            throw;
        }
    }

    private static uint GetWinlogonPidForSession(uint sessionId)
    {
        Log.Information("Retrieving Winlogon PID for session {SessionId}", sessionId);
        
        foreach (var process in Process.GetProcessesByName("winlogon"))
        {
            if ((uint)process.SessionId == sessionId)
            {
                Log.Information("Found Winlogon PID: {WinlogonPid} for session {SessionId}", process.Id, sessionId);
                
                return (uint)process.Id;
            }
        }

        Log.Error("No Winlogon process found for session {SessionId}", sessionId);
        throw new Exception("No winlogon process found for the given session id.");
    }

    private static uint FindTargetSessionId(int targetSessionId)
    {
        Log.Information("Finding target session ID: {TargetSessionId}", targetSessionId);
        
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

        var foundSessionId = targetSessionFound ? (uint)targetSessionId : lastSessionId;
        Log.Information("Target session ID found: {FoundSessionId}", foundSessionId);
        
        return foundSessionId;
    }

    private unsafe bool TryCreateInteractiveProcess(NativeProcessStartInfo startInfo, SafeHandle hUserTokenDup)
    {
        Log.Information("Attempting to create interactive process with StartInfo: {@StartInfo}", startInfo);

        STARTUPINFOW startupInfo = default;
        startupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOW>();

        PROCESS_INFORMATION processInformation = default;

        if (!CreatePipe(out var stdInReadHandle, out var stdInWriteHandle, null, 0))
        {
            Log.Error("Failed to create pipe for standard input.");
            
            return false;
        }

        if (!CreatePipe(out var stdOutReadHandle, out var stdOutWriteHandle, null, 0))
        {
            Log.Error("Failed to create pipe for standard output.");
            
            return false;
        }

        if (!CreatePipe(out var stdErrReadHandle, out var stdErrWriteHandle, null, 0))
        {
            Log.Error("Failed to create pipe for standard error.");
            
            return false;
        }

        if (!SetHandleInformation(stdOutWriteHandle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {
            Log.Error("Failed to set inheritance attribute for the hStdOutput handle.");
            
            return false;
        }

        if (!SetHandleInformation(stdErrWriteHandle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {
            Log.Error("Failed to set inheritance attribute for the hStdError handle.");
            
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
        var result = CreateProcessAsUser(hUserTokenDup, null, ref commandSpan, null, null, startInfo.InheritHandles, dwCreationFlags, null, null, startupInfo, out processInformation);

        if (result)
        {
            Log.Information("Interactive process created successfully. Process ID: {ProcessId}", processInformation.dwProcessId);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var consoleEncoding = Encoding.GetEncoding((int)GetConsoleOutputCP());

            _standardOutput = new StreamReader(new FileStream(stdOutReadHandle, FileAccess.Read), consoleEncoding);
            _standardError = new StreamReader(new FileStream(stdErrReadHandle, FileAccess.Read), consoleEncoding);
            _processHandle = new SafeFileHandle(processInformation.hProcess, true);
            _processId = processInformation.dwProcessId;

            stdInWriteHandle.Close();
            stdOutWriteHandle.Close();
            stdErrWriteHandle.Close();
        }
        else
        {
            Log.Error("Failed to create interactive process.");
        }

        return result;
    }

    private static bool TryGetUserToken(uint sessionId, out SafeFileHandle hUserToken)
    {
        Log.Information("Attempting to get user token for session {SessionId}", sessionId);
        
        var userTokenHandle = default(HANDLE);
        var success = WTSQueryUserToken(sessionId, ref userTokenHandle);
        hUserToken = new SafeFileHandle(userTokenHandle, true);

        if (success)
        {
            Log.Information("User token retrieved successfully for session {SessionId}", sessionId);
        }
        else
        {
            Log.Error("Failed to get user token for session {SessionId}", sessionId);
        }

        return success;
    }

    internal static unsafe List<WTS_SESSION_INFOW> GetActiveSessions()
    {
        Log.Information("Retrieving active sessions.");
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

    public void Close()
    {
        StandardOutput?.Dispose();
        StandardError?.Dispose();
        _processHandle?.Close();
    }
}