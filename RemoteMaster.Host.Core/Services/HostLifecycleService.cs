// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostLifecycleService(IServerHubService serverHubService, ICertificateRequestService certificateRequestService, ISubjectService subjectService, IHostConfigurationService hostConfigurationService) : IHostLifecycleService
{
    private volatile bool _isRegistrationInvoked;

    public async Task RegisterAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        if (hostConfiguration.Host == null)
        {
            throw new ArgumentException("Host configuration must have a non-null Host property.", nameof(hostConfiguration));
        }

        RSA? rsaKeyPair = null;

        try
        {
            await serverHubService.ConnectAsync(hostConfiguration.Server);

            var securityDirectory = EnsureSecurityDirectoryExists();

            Log.Information("Attempting to register host...");
            await InvokeHostRegistration(hostConfiguration, securityDirectory);
        }
        catch (Exception ex)
        {
            Log.Error("Registering host failed: {Message}.", ex.Message);
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
            await serverHubService.ConnectAsync(hostConfiguration.Server);

            Log.Information("Attempting to unregister host...");

            if (await serverHubService.UnregisterHostAsync(hostConfiguration))
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
            Log.Error(ex, "Unregistering host failed: {Message}.", ex.Message);
        }
    }

    public async Task IssueCertificateAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        RSA? rsaKeyPair = null;

        try
        {
            await serverHubService.ConnectAsync(hostConfiguration.Server);

            var ipAddresses = new List<string>
            {
                hostConfiguration.Host.IpAddress
            };

            var distinguishedName = subjectService.GetDistinguishedName(hostConfiguration.Host.Name);
            var csr = certificateRequestService.GenerateSigningRequest(distinguishedName, ipAddresses, out rsaKeyPair);
            var signingRequest = csr.CreateSigningRequest();

            var tcs = new TaskCompletionSource<bool>();

            var securityDirectory = EnsureSecurityDirectoryExists();
            serverHubService.OnReceiveCertificate(certificateBytes => ProcessCertificate(certificateBytes, rsaKeyPair, securityDirectory, tcs));

            Log.Information("Attempting to issue certificate...");
            await serverHubService.IssueCertificateAsync(signingRequest);

            var isCertificateReceived = await tcs.Task;

            if (!isCertificateReceived)
            {
                throw new InvalidOperationException("Certificate processing failed.");
            }

            Log.Information("Certificate issued and processed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error("Issuing certificate failed: {Message}.", ex.Message);
        }
        finally
        {
            rsaKeyPair?.Dispose();
        }
    }

    public async Task UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        try
        {
            await serverHubService.ConnectAsync(hostConfiguration.Server);

            if (await serverHubService.UpdateHostInformationAsync(hostConfiguration))
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
            Log.Error("Update host information failed: {Message}.", ex.Message);
        }
    }

    public async Task<bool> IsHostRegisteredAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        try
        {
            await serverHubService.ConnectAsync(hostConfiguration.Server);
            var isRegistered = await serverHubService.IsHostRegisteredAsync(hostConfiguration);
            
            return isRegistered;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.NetworkUnreachable })
        {
            Log.Warning("Network is unreachable. Assuming host is still registered based on previous state.");
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Error checking host registration status: {Message}", ex.Message);
            
            return true;
        }
    }

    private static string EnsureSecurityDirectoryExists()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var securityDirectory = Path.Combine(programData, "RemoteMaster", "Security");

        if (Directory.Exists(securityDirectory))
        {
            return securityDirectory;
        }

        Directory.CreateDirectory(securityDirectory);
        Log.Debug("Security directory created at {DirectoryPath}.", securityDirectory);

        return securityDirectory;
    }

    private void ProcessCertificate(byte[] certificateBytes, RSA rsaKeyPair, string securityDirectory, TaskCompletionSource<bool> tcs)
    {
        try
        {
            SpinWait.SpinUntil(() => _isRegistrationInvoked);

            if (certificateBytes == null || certificateBytes.Length == 0)
            {
                throw new Exception("Certificate bytes are null or empty.");
            }

            using var certificate = new X509Certificate2(certificateBytes);
            Log.Information("Certificate received with Serial Number: {SerialNumber}.", certificate.SerialNumber);

            var pfxFilePath = Path.Combine(securityDirectory, "certificate.pfx");
            const string pfxPassword = "YourPfxPassword";
            CreatePfxFile(certificateBytes, rsaKeyPair, pfxFilePath, pfxPassword);

            Log.Information("PFX file saved successfully at {Path}.", pfxFilePath);
            tcs.SetResult(true);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred while processing the certificate: {ErrorMessage}.", ex.Message);
            tcs.SetResult(false);
        }
    }

    private async Task InvokeHostRegistration(HostConfiguration hostConfiguration, string securityDirectory)
    {
        var tcsGuid = new TaskCompletionSource<Guid>();
        
        serverHubService.OnReceiveHostGuid(guid => {
            Log.Information("Host GUID received: {GUID}.", guid);

            hostConfiguration.HostGuid = guid;

            hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

            tcsGuid.SetResult(guid);
        });

        if (await serverHubService.RegisterHostAsync(hostConfiguration))
        {
            _isRegistrationInvoked = true;
            
            Log.Information("Host registration invoked successfully. Waiting for the certificate...");

            var publicKey = await serverHubService.GetPublicKeyAsync();

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new InvalidOperationException("Failed to obtain JWT public key.");
            }

            var publicKeyPath = Path.Combine(securityDirectory, "public_key.pem");
            SavePublicKey(publicKey, publicKeyPath);

            Log.Information("Host registration successful with certificate received.");
        }
        else
        {
            Log.Warning("Host registration was not successful.");
        }
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
            Log.Information("Public key saved successfully at {Path}.", filePath);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to save public key: {ErrorMessage}.", ex.Message);
        }
    }

    public async Task GetCaCertificateAsync()
    {
        try
        {
            Log.Information("Requesting CA certificate's public part...");

            var tcs = new TaskCompletionSource<bool>();

            serverHubService.OnReceiveCertificate(caCertificateBytes =>
            {
                if (caCertificateBytes == null || caCertificateBytes.Length == 0)
                {
                    Log.Error("Received CA certificate is null or empty.");
                    tcs.SetResult(false);
                }
                else
                {
                    try
                    {
                        using var caCertificate = new X509Certificate2(caCertificateBytes);

                        var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);

                        store.Open(OpenFlags.ReadWrite);
                        store.Add(caCertificate);
                        store.Close();

                        Log.Information("CA certificate imported successfully into the certificate store.");
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("An error occurred while importing the CA certificate: {ErrorMessage}.", ex.Message);
                        tcs.SetResult(false);
                    }
                }
            });

            var success = await serverHubService.GetCaCertificateAsync();

            if (!success)
            {
                throw new InvalidOperationException("Failed to request or process CA certificate.");
            }

            await tcs.Task;

            Log.Information("CA certificate's public part received and processed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to get CA certificate: {Message}.", ex.Message);
        }
    }
}
