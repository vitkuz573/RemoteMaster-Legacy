// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostLifecycleService(IServerHubService serverHubService, ICertificateRequestService certificateRequestService, ISubjectService subjectService) : IHostLifecycleService
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

            var ipAddresses = new List<string>
            {
                hostConfiguration.Host.IpAddress
            };

            var distinguishedName = subjectService.GetDistinguishedName(hostConfiguration.Host.Name);
            var csr = certificateRequestService.GenerateSigningRequest(distinguishedName, ipAddresses, out rsaKeyPair);
            var signingRequest = csr.CreateSigningRequest();

            var securityDirectory = EnsureSecurityDirectoryExists();

            var tcs = new TaskCompletionSource<bool>();
            serverHubService.OnReceiveCertificate(certificateBytes => ProcessCertificate(certificateBytes, rsaKeyPair, securityDirectory, tcs));

            Log.Information("Attempting to register host...");
            await InvokeHostRegistration(hostConfiguration, signingRequest, tcs, securityDirectory);
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
        catch (Exception ex)
        {
            Log.Error("Error checking host registration status: {Message}", ex.Message);

            return false;
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

    private async Task InvokeHostRegistration(HostConfiguration hostConfiguration, byte[] signingRequest, TaskCompletionSource<bool> tcs, string securityDirectory)
    {
        if (await serverHubService.RegisterHostAsync(hostConfiguration, signingRequest))
        {
            _isRegistrationInvoked = true;
            Log.Information("Host registration invoked successfully. Waiting for the certificate...");
            var isCertificateReceived = await tcs.Task;

            if (!isCertificateReceived)
            {
                throw new InvalidOperationException("Certificate processing failed.");
            }

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
}
