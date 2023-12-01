// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostLifecycleService(ICertificateRequestService certificateRequestService, ISubjectNameService subjectInfoService) : IHostLifecycleService
{
    public async Task RegisterAsync(HostConfiguration config, string hostName, string ipAddress, string macAddress)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            Log.Information("Attempting to register host...");

            var ipAddresses = new List<string>
            {
                ipAddress
            };

            RSA rsaKeyPair;
            var subjectName = subjectInfoService.GetDistinguishedName(hostName);
            var csr = certificateRequestService.GenerateCSR(subjectName, ipAddresses, out rsaKeyPair);

            connection.On<byte[]>("ReceiveCertificate", certificateBytes =>
            {
                Log.Information("Received certificate from server.");

                var pfxFilePath = @"C:\certificate.pfx";
                var pfxPassword = "YourPfxPassword";
                CreatePfxFile(certificateBytes, rsaKeyPair, pfxFilePath, pfxPassword);

                Log.Information("PFX file created successfully.");
            });

            var signingRequest = csr.CreateSigningRequest();

            if (await connection.InvokeAsync<bool>("RegisterHostAsync", hostName, ipAddress, macAddress, config, signingRequest))
            {
                Log.Information("Host registration successful.");
            }
            else
            {
                Log.Warning("Host registration was not successful.");
            }

            rsaKeyPair.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error("Registering host failed: {Message}", ex.Message);
        }
    }

    public async Task UnregisterAsync(HostConfiguration config, string hostName)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            Log.Information("Attempting to unregister host...");

            if (await connection.InvokeAsync<bool>("UnregisterHostAsync", hostName, config))
            {
                Log.Information("Host unregistration successful.");
            }
            else
            {
                Log.Warning("Host unregistration was not successful.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unregistering host failed: {Message}", ex.Message);
        }
    }

    public async Task UpdateHostInformationAsync(HostConfiguration config, string hostname, string ipAddress)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            if (await connection.InvokeAsync<bool>("UpdateHostInformationAsync", hostname, config.Group, ipAddress))
            {
                Log.Information("Host information updated successful.");
            }
            else
            {
                Log.Warning("Host information update was not successful.");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Update host information failed: {Message}", ex.Message);
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
