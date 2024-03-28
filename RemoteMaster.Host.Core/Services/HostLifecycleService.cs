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
    private bool _isRenewalProcess = false;

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
            var securityDirectory = EnsureSecurityDirectoryExists();

            await serverHubService.ConnectAsync(hostConfiguration.Server);

            Log.Information("Attempting to register host...");

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

                try
                {
                    File.WriteAllText(publicKeyPath, publicKey);
                    Log.Information("Public key saved successfully at {Path}.", publicKeyPath);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to save public key: {ErrorMessage}.", ex.Message);
                }

                Log.Information("Host registration successful with certificate received.");
            }
            else
            {
                Log.Warning("Host registration was not successful.");
            }
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

            Log.Information("Removing existing certificates...");
            RemoveExistingCertificate();

            var csr = certificateRequestService.GenerateSigningRequest(distinguishedName, ipAddresses, out rsaKeyPair);
            var signingRequest = csr.CreateSigningRequest();

            var tcs = new TaskCompletionSource<bool>();

            serverHubService.OnReceiveCertificate(certificateBytes => ProcessCertificate(certificateBytes, rsaKeyPair, tcs));

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

    public async Task RenewCertificateAsync(HostConfiguration hostConfiguration)
    {
        _isRenewalProcess = true;

        try
        {
            await IssueCertificateAsync(hostConfiguration);
        }
        finally
        {
            _isRenewalProcess = false;
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

    private void ProcessCertificate(byte[] certificateBytes, RSA rsaKeyPair, TaskCompletionSource<bool> tcs)
    {
        X509Certificate2? tempCertificate = null;

        try
        {
            if (!_isRenewalProcess)
            {
                SpinWait.SpinUntil(() => _isRegistrationInvoked);
            }

            if (certificateBytes == null || certificateBytes.Length == 0)
            {
                Log.Error("Certificate bytes are null or empty.");
                tcs.SetResult(false);
                return;
            }

            Log.Information("Received certificate bytes, starting processing...");

            tempCertificate = new X509Certificate2(certificateBytes, (string?)null, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            Log.Information("Temporary certificate created successfully.");

            LogCertificateDetails(tempCertificate);

            var cspParams = new CspParameters
            {
                KeyContainerName = Guid.NewGuid().ToString(),
                Flags = CspProviderFlags.UseMachineKeyStore,
                KeyNumber = (int)KeyNumber.Exchange
            };

            using var rsaProvider = new RSACryptoServiceProvider(cspParams);
            var rsaParameters = rsaKeyPair.ExportParameters(true);
            rsaProvider.ImportParameters(rsaParameters);

            var certificateWithPrivateKey = tempCertificate.CopyWithPrivateKey(rsaProvider);
            Log.Information("Certificate with private key prepared.");

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificateWithPrivateKey);
                Log.Information("Certificate with private key imported successfully into the certificate store.");
            }

            tcs.SetResult(true);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred while processing the certificate: {ErrorMessage}.", ex.Message);
            tcs.SetResult(false);
        }
        finally
        {
            tempCertificate?.Dispose();
        }
    }

    private static void LogCertificateDetails(X509Certificate2 certificate)
    {
        Log.Information("Certificate Details:");

        Log.Information("    Subject: {Subject}", certificate.Subject);
        Log.Information("    Issuer: {Issuer}", certificate.Issuer);
        Log.Information("    Valid From: {ValidFrom}", certificate.NotBefore);
        Log.Information("    Valid To: {ValidTo}", certificate.NotAfter);
        Log.Information("    Serial Number: {SerialNumber}", certificate.SerialNumber);
        Log.Information("    Thumbprint: {Thumbprint}", certificate.Thumbprint);
        Log.Information("    Version: {Version}", certificate.Version);
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

                        LogCertificateDetails(caCertificate);

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

    private static void RemoveExistingCertificate()
    {
        Log.Information("Starting the process of removing existing certificates...");

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);

        var existingCertificates = store.Certificates.Find(X509FindType.FindBySubjectName, Environment.MachineName, false);

        if (existingCertificates.Count > 0)
        {
            Log.Information("Found {Count} certificates to remove.", existingCertificates.Count);

            foreach (var cert in existingCertificates)
            {
                try
                {
                    store.Remove(cert);
                    Log.Information("Successfully removed certificate with serial number: {SerialNumber}.", cert.SerialNumber);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to remove certificate with serial number: {SerialNumber}. Error: {Message}", cert.SerialNumber, ex.Message);
                }
            }
        }
        else
        {
            Log.Information("No certificates found to remove.");
        }

        store.Close();

        Log.Information("Finished removing existing certificates.");
    }
}
