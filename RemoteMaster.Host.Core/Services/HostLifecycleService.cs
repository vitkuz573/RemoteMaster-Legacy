// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Net.Sockets;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Services;

public class HostLifecycleService(IApiService apiService, IFileSystem fileSystem, IApplicationPathProvider applicationPathProvider, ILogger<HostLifecycleService> logger) : IHostLifecycleService
{
    public async Task<bool> RegisterAsync(bool force)
    {
        RSA? rsaKeyPair = null;

        try
        {
            var jwtDirectory = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, "JWT");

            if (!fileSystem.Directory.Exists(jwtDirectory))
            {
                fileSystem.Directory.CreateDirectory(jwtDirectory);
            }

            logger.LogInformation("Attempting to register host...");

            var isRegistered = await apiService.RegisterHostAsync(force);

            if (isRegistered)
            {
                var jwtPublicKey = await apiService.GetJwtPublicKeyAsync();

                if (jwtPublicKey == null || jwtPublicKey.Length == 0)
                {
                    throw new InvalidOperationException("Failed to obtain JWT public key.");
                }

                var publicKeyPath = fileSystem.Path.Combine(jwtDirectory, "public_key.der");

                try
                {
                    await fileSystem.File.WriteAllBytesAsync(publicKeyPath, jwtPublicKey);

                    logger.LogInformation("Public key saved successfully at {Path}.", publicKeyPath);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to save public key: {ErrorMessage}.", ex.Message);
                    throw;
                }

                logger.LogInformation("Host registration successful with certificate received.");

                return true;
            }
            else
            {
                logger.LogWarning("Host registration was not successful.");

                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Registering host failed: {Message}.", ex.Message);

            return false;
        }
        finally
        {
            rsaKeyPair?.Dispose();
        }
    }

    public async Task<bool> UnregisterAsync()
    {
        try
        {
            logger.LogInformation("Attempting to unregister host...");

            var isUnregistered = await apiService.UnregisterHostAsync();

            if (isUnregistered)
            {
                logger.LogInformation("Host unregister successful.");

                return true;
            }

            logger.LogWarning("Host unregister was not successful.");

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unregistering host failed: {Message}.", ex.Message);

            return false;
        }
    }

    public async Task<bool> UpdateHostInformationAsync()
    {
        try
        {
            var isUpdated = await apiService.UpdateHostInformationAsync();

            if (isUpdated)
            {
                logger.LogInformation("Host information updated successfully.");

                return true;
            }

            logger.LogWarning("Host information update was not successful.");

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError("Update host information failed: {Message}.", ex.Message);

            return false;
        }
    }

    public async Task<bool> IsHostRegisteredAsync()
    {
        try
        {
            return await apiService.IsHostRegisteredAsync();
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.NetworkUnreachable or SocketError.ConnectionRefused })
        {
            logger.LogDebug("Network error (unreachable or connection refused). Assuming host is still registered based on previous state.");

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Error checking host registration status: {Message}", ex.Message);

            return true;
        }
    }

    public async Task<AddressDto> GetOrganizationAddressAsync(string name)
    {
        try
        {
            logger.LogInformation("Requesting organization address for organization: {Organization}", name);

            var organization = await apiService.GetOrganizationAsync(name) ?? throw new InvalidOperationException($"Failed to retrieve address for organization: {name}");
            logger.LogInformation("Successfully retrieved address for organization: {Organization}", organization);

            return organization.Address;
        }
        catch (Exception ex)
        {
            logger.LogError("Error retrieving organization address: {Message}", ex.Message);
            throw;
        }
    }
}
