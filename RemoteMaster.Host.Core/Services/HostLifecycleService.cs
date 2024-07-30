// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostLifecycleService(ICertificateRequestService certificateRequestService, ISubjectService subjectService, ICertificateLoaderService certificateLoaderService, IApiService apiService) : IHostLifecycleService
{
    public async Task RegisterAsync()
    {
        RSA? rsaKeyPair = null;

        try
        {
            var jwtDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "Security", "JWT");

            if (!Directory.Exists(jwtDirectory))
            {
                Directory.CreateDirectory(jwtDirectory);
            }

            Log.Information("Attempting to register host...");

            var isRegistered = await apiService.RegisterHostAsync();

            if (isRegistered)
            {
                var jwtPublicKey = await apiService.GetJwtPublicKeyAsync();

                if (jwtPublicKey == null || jwtPublicKey.Length == 0)
                {
                    throw new InvalidOperationException("Failed to obtain JWT public key.");
                }

                var publicKeyPath = Path.Combine(jwtDirectory, "public_key.der");

                try
                {
                    await File.WriteAllBytesAsync(publicKeyPath, jwtPublicKey);

                    Log.Information("Public key saved successfully at {Path}.", publicKeyPath);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to save public key: {ErrorMessage}.", ex.Message);
                    throw;
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

    public async Task UnregisterAsync()
    {
        try
        {
            Log.Information("Attempting to unregister host...");

            var isUnregistered = await apiService.UnregisterHostAsync();

            if (isUnregistered)
            {
                Log.Information("Host unregister successful.");
            }
            else
            {
                Log.Warning("Host unregister was not successful.");
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
            var ipAddresses = new string[]
            {
                hostConfiguration.Host!.IpAddress
            };

            var distinguishedName = subjectService.GetDistinguishedName(hostConfiguration.Host.Name);

            Log.Information("Removing existing certificates...");

            RemoveExistingCertificate();

            var signingRequest = certificateRequestService.GenerateSigningRequest(distinguishedName, ipAddresses, out rsaKeyPair);

            Log.Information("Attempting to issue certificate...");

            var certificate = await apiService.IssueCertificateAsync(signingRequest);

            if (certificate == null || certificate.Length == 0)
            {
                throw new InvalidOperationException("Certificate processing failed.");
            }

            ProcessCertificate(certificate, rsaKeyPair);

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

    public void RemoveCertificate()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);

        var certificate = store.Certificates
            .Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false)
            .FirstOrDefault(cert => cert.HasPrivateKey);

        if (certificate != null)
        {
            store.Remove(certificate);

            Log.Information("Certificate with private key removed successfully from certificate store.");
        }
        else
        {
            Log.Warning("No certificate with a private key found in the certificate store.");
        }
    }

    public async Task UpdateHostInformationAsync()
    {
        try
        {
            var isUpdated = await apiService.UpdateHostInformationAsync();

            if (isUpdated)
            {
                Log.Information("Host information updated successfully.");
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

    public async Task<bool> IsHostRegisteredAsync()
    {
        try
        {
            return await apiService.IsHostRegisteredAsync();
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

    private void ProcessCertificate(byte[] certificateBytes, RSA rsaKeyPair)
    {
        X509Certificate2? tempCertificate = null;

        try
        {
            if (certificateBytes.Length == 0)
            {
                Log.Error("Certificate bytes are empty.");

                return;
            }

            Log.Information("Received certificate bytes, starting processing...");

            tempCertificate = new X509Certificate2(certificateBytes, (string?)null, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            Log.Information("Temporary certificate created successfully.");

            LogCertificateDetails(tempCertificate);

            X509Certificate2? certificateWithPrivateKey = null;

            if (OperatingSystem.IsWindows())
            {
                var cspParams = new CspParameters
                {
                    KeyContainerName = Guid.NewGuid().ToString(),
                    Flags = CspProviderFlags.UseMachineKeyStore,
                    KeyNumber = (int)KeyNumber.Exchange
                };

                using var rsaProvider = new RSACryptoServiceProvider(cspParams);

                var rsaParameters = rsaKeyPair.ExportParameters(true);
                rsaProvider.ImportParameters(rsaParameters);

                certificateWithPrivateKey = tempCertificate.CopyWithPrivateKey(rsaProvider);
                certificateWithPrivateKey.FriendlyName = "RemoteMaster Host Certificate";

                Log.Information("Certificate with private key prepared.");
            }

            if (certificateWithPrivateKey != null)
            {
                using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificateWithPrivateKey);

                Log.Information("Certificate with private key imported successfully into the certificate store.");
            }
            else
            {
                Log.Error("Failed to create a certificate with private key.");

                return;
            }

            certificateLoaderService.LoadCertificate();
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred while processing the certificate: {ErrorMessage}.", ex.Message);
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

            var caCertificateData = await apiService.GetCaCertificateAsync();

            if (caCertificateData == null || caCertificateData.Length == 0)
            {
                Log.Error("Received CA certificate is null or empty.");
                throw new InvalidOperationException("Failed to request or process CA certificate.");
            }

            try
            {
                using var caCertificate = new X509Certificate2(caCertificateData);

                LogCertificateDetails(caCertificate);

                var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(caCertificate);
                store.Close();

                Log.Information("CA certificate imported successfully into the certificate store.");
            }
            catch (Exception ex)
            {
                Log.Error("An error occurred while importing the CA certificate: {ErrorMessage}.", ex.Message);
                throw;
            }

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

        var existingCertificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);

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
