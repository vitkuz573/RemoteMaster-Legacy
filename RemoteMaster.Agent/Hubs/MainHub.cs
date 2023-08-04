// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Shared.Native.Windows;

namespace RemoteMaster.Agent.Hubs;

[SupportedOSPlatform("windows6.0.6000")]
public class MainHub : Hub
{
    private readonly string _serverPath;
    private readonly string _serverName;

    private readonly ILogger<MainHub> _logger;

    public MainHub(ILogger<MainHub> logger)
    {
        _logger = logger;
#if DEBUG
        _serverPath = @"C:\sc\RemoteMaster.Server\RemoteMaster.Server.exe";
#else
        _serverPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"RemoteMaster\Server\RemoteMaster.Server.exe");
#endif
        _serverName = Path.GetFileNameWithoutExtension(_serverPath);
    }

    public async override Task OnConnectedAsync()
    {
        if (!IsServerRunning())
        {
            var certificateThumbprint = "1A196F7ECB0087FBD09F9BDDFA1F66FE1996F90F";
            
            if (IsAuthenticodeVerified(_serverPath, certificateThumbprint))
            {
                try
                {
                    ProcessHelper.OpenInteractiveProcess(_serverPath, -1, true, "default", true, out _);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting RemoteMaster Server");
                }
            }
            else
            {
                // The server file has been tampered with or the digital signature is not valid.
                await Clients.Caller.SendAsync("ServerTampered", "The RemoteMaster server appears to be tampered with or its digital signature is not valid. Please contact support.");
            }
        }

        await base.OnConnectedAsync();
    }

    private bool IsServerRunning() => Process.GetProcessesByName(_serverName).Length > 0;

    private bool IsAuthenticodeVerified(string filename, string expectedThumbprint)
    {
        using var cert = X509Certificate.CreateFromSignedFile(filename);
        using var cert2 = new X509Certificate2(cert);
        using var chain = new X509Chain();

        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

        var isChainValid = chain.Build(cert2);
        
        if (isChainValid)
        {
            if (cert2.Thumbprint.Equals(expectedThumbprint, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Digital signature is valid.");
                return true;
            }
            else
            {
                _logger.LogWarning("Digital signature is valid but not from the expected certificate.");
                return false;
            }
        }
        else
        {
            _logger.LogWarning("Digital signature is not valid.");
            return false;
        }
    }
}
