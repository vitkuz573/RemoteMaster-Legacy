// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Hubs;

/// <summary>
/// Hub for managing various operations related to hosts and certificates.
/// </summary>
public class ManagementHub(ICertificateService certificateService, ICaCertificateService caCertificateService, IDatabaseService databaseService, INotificationService notificationService) : Hub<IManagementClient>
{
    /// <summary>
    /// Retrieves the public key.
    /// </summary>
    /// <returns>The public key as a byte array if found, otherwise null.</returns>
    public async Task<byte[]?> GetPublicKey()
    {
        try
        {
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");

            if (File.Exists(publicKeyPath))
            {
                return await File.ReadAllBytesAsync(publicKeyPath);
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while reading public key file.");
            await notificationService.SendNotificationAsync($"Error while reading public key file: {ex.Message}");

            return null;
        }
    }

    /// <summary>
    /// Retrieves the list of host move requests.
    /// </summary>
    /// <returns>The list of host move requests.</returns>
    private static async Task<List<HostMoveRequest>> GetHostMoveRequestsAsync()
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var hostMoveRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");

        if (File.Exists(hostMoveRequestsFilePath))
        {
            var json = await File.ReadAllTextAsync(hostMoveRequestsFilePath);

            return JsonSerializer.Deserialize<List<HostMoveRequest>>(json) ?? [];
        }

        return [];
    }

    /// <summary>
    /// Saves the list of host move requests.
    /// </summary>
    /// <param name="hostMoveRequests">The list of host move requests.</param>
    private static async Task SaveHostMoveRequestsAsync(List<HostMoveRequest> hostMoveRequests)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var hostMoveRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");
        var updatedJson = JsonSerializer.Serialize(hostMoveRequests);

        await File.WriteAllTextAsync(hostMoveRequestsFilePath, updatedJson);
    }

    /// <summary>
    /// Retrieves a host move request by MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host.</param>
    /// <returns>The host move request if found, otherwise null.</returns>
    public async Task<HostMoveRequest?> GetHostMoveRequest(string macAddress)
    {
        var hostMoveRequests = await GetHostMoveRequestsAsync();

        return hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Acknowledges a host move request by removing it from the list.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host.</param>
    public async Task AcknowledgeMoveRequest(string macAddress)
    {
        var hostMoveRequests = await GetHostMoveRequestsAsync();
        var requestToRemove = hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

        if (requestToRemove != null)
        {
            hostMoveRequests.Remove(requestToRemove);
            await SaveHostMoveRequestsAsync(hostMoveRequests);

            Log.Information("Acknowledged move request for host with MAC address: {MacAddress}", macAddress);
            await notificationService.SendNotificationAsync($"Acknowledged move request for host with MAC address: {macAddress}");
        }
    }

    /// <summary>
    /// Issues a certificate based on the provided CSR bytes.
    /// </summary>
    /// <param name="csrBytes">The CSR bytes.</param>
    /// <returns>True if the certificate is issued successfully, otherwise false.</returns>
    public async Task<bool> IssueCertificateAsync(byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(csrBytes, nameof(csrBytes));

        try
        {
            var certificate = certificateService.IssueCertificate(csrBytes);
            await Clients.Caller.ReceiveCertificate(certificate.Export(X509ContentType.Pfx));

            Log.Information("Certificate issued successfully.");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while issuing certificate.");
            await notificationService.SendNotificationAsync($"Error while issuing certificate: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    /// Retrieves the CA certificate.
    /// </summary>
    /// <returns>True if the CA certificate is retrieved successfully, otherwise false.</returns>
    public async Task<bool> GetCaCertificateAsync()
    {
        try
        {
            var caCertificatePublicPart = caCertificateService.GetCaCertificate(X509ContentType.Cert);

            if (caCertificatePublicPart != null)
            {
                await Clients.Caller.ReceiveCertificate(caCertificatePublicPart.RawData);

                Log.Information("CA certificate retrieved successfully.");

                return true;
            }
            else
            {
                Log.Warning("CA certificate retrieval failed.");
                await notificationService.SendNotificationAsync("CA certificate retrieval failed.");

                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while sending CA certificate public part.");
            await notificationService.SendNotificationAsync($"Error while sending CA certificate public part: {ex.Message}");

            return false;
        }
    }
}
