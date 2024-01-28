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
    private volatile bool _isRegistrationInvoked = false;

    public async Task RegisterAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var tcs = new TaskCompletionSource<bool>();
        RSA rsaKeyPair = null;

        try
        {
            var connection = await ConnectToServerHub($"http://{hostConfiguration.Server}:5254");

            var ipAddresses = new List<string>
            {
                hostConfiguration.Host.IPAddress
            };

            var subjectName = subjectService.GetDistinguishedName(hostConfiguration.Host.Name);
            var csr = certificateRequestService.GenerateCSR(subjectName, ipAddresses, out rsaKeyPair);
            var signingRequest = csr.CreateSigningRequest();

            connection.On<byte[]>("ReceiveCertificate", certificateBytes =>
            {
                try
                {
                    SpinWait.SpinUntil(() => _isRegistrationInvoked);

                    if (certificateBytes == null || certificateBytes.Length == 0)
                    {
                        throw new Exception("Certificate bytes are null or empty.");
                    }

                    var certificate = new X509Certificate2(certificateBytes);
                    Log.Information("Certificate Serial Number: {SerialNumber}", certificate.SerialNumber);
                    Log.Information("Certificate Subject Name: {SubjectName}", certificate.Subject);

                    var pfxFilePath = @"C:\ProgramData\RemoteMaster\Security\certificate.pfx";
                    var pfxPassword = "YourPfxPassword";
                    CreatePfxFile(certificateBytes, rsaKeyPair, pfxFilePath, pfxPassword);

                    Log.Information("PFX file created successfully.");
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    Log.Error("An error occurred while processing the certificate: {ErrorMessage}", ex.Message);
                    tcs.SetResult(false);
                }
            });

            Log.Information("Attempting to register host...");

            if (await connection.InvokeAsync<bool>("RegisterHostAsync", hostConfiguration, signingRequest))
            {
                _isRegistrationInvoked = true;
                Log.Information("Host registration invoked successfully. Waiting for the certificate...");
                var isCertificateReceived = await tcs.Task;

                if (!isCertificateReceived)
                {
                    throw new InvalidOperationException("Certificate processing failed.");
                }

                var publicKey = await connection.InvokeAsync<string>("GetPublicKey");

                if (string.IsNullOrEmpty(publicKey))
                {
                    throw new InvalidOperationException("Failed to obtain JWT public key.");
                }

                var publicKeyPath = @"C:\ProgramData\RemoteMaster\Security\public_key.pem";
                SavePublicKey(publicKey, publicKeyPath);

                Log.Information("Host registration successful with certificate received.");
            }
            else
            {
                Log.Warning("Host registration was not successful.");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Registering host failed: {Message}", ex.Message);
        }
        finally
        {
            rsaKeyPair?.Dispose();
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

    public async Task UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        try
        {
            var connection = await ConnectToServerHub($"http://{hostConfiguration.Server}:5254");

            if (await connection.InvokeAsync<bool>("UpdateHostInformationAsync", hostConfiguration))
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

    public async Task<bool> IsHostRegisteredAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        try
        {
            var connection = await ConnectToServerHub($"http://{hostConfiguration.Server}:5254");

            Log.Information("Checking if host is registered...");

            var isRegistered = await connection.InvokeAsync<bool>("IsHostRegisteredAsync", hostConfiguration);

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

    private static void SavePublicKey(string publicKey, string filePath)
    {
        try
        {
            File.WriteAllText(filePath, publicKey);
            Log.Information("Public key saved successfully at {Path}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to save public key: {ErrorMessage}", ex.Message);
        }
    }
}
