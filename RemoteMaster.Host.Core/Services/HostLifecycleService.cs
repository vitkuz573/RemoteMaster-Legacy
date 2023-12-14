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

public class HostLifecycleService(ICertificateRequestService certificateRequestService, ISubjectService subjectService) : IHostLifecycleService
{
    public async Task RegisterAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        try
        {
            var connection = await ConnectToServerHub($"http://{hostConfiguration.Server}:5254");

            Log.Information("Attempting to register host...");

            var ipAddresses = new List<string>
            {
                hostConfiguration.Host.IPAddress
            };

            var subjectName = subjectService.GetDistinguishedName(hostConfiguration.Host.Name);
            var csr = certificateRequestService.GenerateCSR(subjectName, ipAddresses, out var rsaKeyPair);

            connection.On<byte[]>("ReceiveCertificate", certificateBytes =>
            {
                Log.Information("Received certificate from server.");

                try
                {
                    var certificate = new X509Certificate2(certificateBytes);

                    // Log the serial number and subject name of the certificate
                    Log.Information("Certificate Serial Number: {SerialNumber}", certificate.SerialNumber);
                    Log.Information("Certificate Subject Name: {SubjectName}", certificate.Subject);
                }
                catch (Exception ex)
                {
                    Log.Error("An error occurred while processing the certificate: {ErrorMessage}", ex.Message);
                }

                var pfxFilePath = @"C:\certificate.pfx";
                var pfxPassword = "YourPfxPassword";
                CreatePfxFile(certificateBytes, rsaKeyPair, pfxFilePath, pfxPassword);

                Log.Information("PFX file created successfully.");
            });

            var signingRequest = csr.CreateSigningRequest();

            if (await connection.InvokeAsync<bool>("RegisterHostAsync", hostConfiguration, signingRequest))
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
            Log.Error(ex, "Registering host failed: {Message}", ex.Message);
        }
    }

    public async Task UnregisterAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        try
        {
            var connection = await ConnectToServerHub($"http://{hostConfiguration.Server}:5254");

            Log.Information("Attempting to unregister host...");

            if (await connection.InvokeAsync<bool>("UnregisterHostAsync", hostConfiguration))
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

    public async Task UpdateHostInformationAsync(HostConfiguration hostConfiguration, string hostname, string ipAddress, string macAddress)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        try
        {
            var connection = await ConnectToServerHub($"http://{hostConfiguration.Server}:5254");

            if (await connection.InvokeAsync<bool>("UpdateHostInformationAsync", hostConfiguration, hostname, ipAddress, macAddress))
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

    public async Task<bool> IsHostRegisteredAsync(HostConfiguration config, string hostName)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            Log.Information("Checking if host is registered...");

            var isRegistered = await connection.InvokeAsync<bool>("IsHostRegisteredAsync", hostName);

            if (isRegistered)
            {
                Log.Information("Host is registered.");
            }
            else
            {
                Log.Warning("Host is not registered.");
            }

            return isRegistered;
        }
        catch (Exception ex)
        {
            Log.Error("Error checking host registration status: {Message}", ex.Message);

            return false;
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
