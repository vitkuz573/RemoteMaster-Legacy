// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
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
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            _logger.LogInformation("Attempting to register host...");

            var ipAddresses = new List<string>
            {
                ipAddress
            };

            var csr = _certificateRequestService.GenerateCSR($"CN={hostName}", ipAddresses, out var keyPair);

            File.WriteAllBytes(@"C:\csr.csr", csr.GetDerEncoded());

            connection.On<byte[]>("ReceiveCertificate", certificateBytes =>
            {
                _logger.LogInformation("Received certificate from server.");

                var pfxFilePath = @"C:\certificate.pfx";
                var pfxPassword = "YourPfxPassword";
                CreatePfxFile(certificateBytes, keyPair, pfxFilePath, pfxPassword);

                _logger.LogInformation("PFX file created successfully.");
            });

            if (await connection.InvokeAsync<bool>("RegisterHostAsync", hostName, ipAddress, macAddress, config.Group, csr.GetDerEncoded()))
            {
                _logger.LogInformation("Host registration successful.");
            }
            else
            {
                _logger.LogWarning("Host registration was not successful.");
            }
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
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

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
            _logger.LogError("Unregistering host failed: {Message}", ex.Message);
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

    private static void CreatePfxFile(byte[] certificateBytes, AsymmetricCipherKeyPair keyPair, string outputPfxPath, string password)
    {
        var cert = new X509CertificateParser().ReadCertificate(certificateBytes);

        var storeBuilder = new Pkcs12StoreBuilder();
        var store = storeBuilder.Build();

        store.SetKeyEntry("key", new AsymmetricKeyEntry(keyPair.Private), new[] { new X509CertificateEntry(cert) });

        using var fs = new FileStream(outputPfxPath, FileMode.Create);
        store.Save(fs, password.ToCharArray(), new SecureRandom());
    }
}
