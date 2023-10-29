// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostLifecycleService : IHostLifecycleService
{
    private readonly ICertificateRequestService _certificateRequestService;
    private readonly ILogger<HostLifecycleService> _logger;

    public HostLifecycleService(ICertificateRequestService certificateRequestService, ILogger<HostLifecycleService> logger)
    {
        _certificateRequestService = certificateRequestService;
        _logger = logger;
    }

    public async Task RegisterAsync(HostConfiguration config, string hostName, string ipAddress, string macAddress)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"https://{config.Server}:5254");

            _logger.LogInformation("Attempting to register host...");

            var ipAddresses = new List<string>
            {
                ipAddress
            };

            RSA rsaKeyPair;
            var csr = _certificateRequestService.GenerateCSR(hostName, "RemoteMaster", "Kurgan", "Kurgan Oblast", "RU", ipAddresses, out rsaKeyPair);

            connection.On<byte[]>("ReceiveCertificate", certificateBytes =>
            {
                _logger.LogInformation("Received certificate from server.");

                var pfxFilePath = @"C:\certificate.pfx";
                var pfxPassword = "YourPfxPassword";
                CreatePfxFile(certificateBytes, rsaKeyPair, pfxFilePath, pfxPassword);

                _logger.LogInformation("PFX file created successfully.");
            });

            if (await connection.InvokeAsync<bool>("RegisterHostAsync", hostName, ipAddress, macAddress, config.Group, csr.CreateSigningRequest()))
            {
                _logger.LogInformation("Host registration successful.");
            }
            else
            {
                _logger.LogWarning("Host registration was not successful.");
            }

            rsaKeyPair.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError("Registering host failed: {Message}", ex.Message);
        }
    }

    public async Task UnregisterAsync(HostConfiguration config, string hostName)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"https://{config.Server}:5254");

            _logger.LogInformation("Attempting to unregister host...");

            if (await connection.InvokeAsync<bool>("UnregisterHostAsync", hostName, config.Group))
            {
                _logger.LogInformation("Host unregistration successful.");
            }
            else
            {
                _logger.LogWarning("Host unregistration was not successful.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unregistering host failed: {Message}", ex.Message);
        }
    }

    public async Task UpdateHostInformationAsync(HostConfiguration config, string hostname, string ipAddress)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"https://{config.Server}:5254");

            if (await connection.InvokeAsync<bool>("UpdateHostInformationAsync", hostname, config.Group, ipAddress))
            {
                _logger.LogInformation("Host information updated successful.");
            }
            else
            {
                _logger.LogWarning("Host information update was not successful.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Update host information failed: {Message}", ex.Message);
        }
    }

    private static async Task<HubConnection> ConnectToServerHub(string serverUrl)
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/hubs/management")
            .Build();

        await hubConnection.StartAsync();

        return hubConnection;
    }

    private static void CreatePfxFile(byte[] certificateBytes, RSA rsaKeyPair, string outputPfxPath, string password)
    {
        using var certificate = new X509Certificate2(certificateBytes);
        var pfxBytes = certificate.CopyWithPrivateKey(rsaKeyPair).Export(X509ContentType.Pfx, password);

        File.WriteAllBytes(outputPfxPath, pfxBytes);
    }
}
