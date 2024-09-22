// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using System.Text.Json;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostMoveService(IEventNotificationService eventNotificationService) : IHostMoveRequestService
{
    private static readonly string ProgramDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    private static readonly string HostMoveRequestsFilePath = Path.Combine(ProgramDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");

    public async Task<Result<List<HostMoveRequest>>> GetHostMoveRequestsAsync()
    {
        try
        {
            if (!File.Exists(HostMoveRequestsFilePath))
            {
                return Result.Ok(new List<HostMoveRequest>());
            }

            var json = await File.ReadAllTextAsync(HostMoveRequestsFilePath);
            var hostMoveRequests = JsonSerializer.Deserialize<List<HostMoveRequest>>(json) ?? [];

            return Result.Ok(hostMoveRequests);
        }
        catch (Exception ex)
        {
            return Result.Fail<List<HostMoveRequest>>("Failed to retrieve host move requests.")
                         .WithError(ex.Message);
        }
    }

    public async Task<Result> SaveHostMoveRequestsAsync(List<HostMoveRequest> hostMoveRequests)
    {
        try
        {
            var updatedJson = JsonSerializer.Serialize(hostMoveRequests);
            await File.WriteAllTextAsync(HostMoveRequestsFilePath, updatedJson);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("Failed to save host move requests.")
                         .WithError(ex.Message);
        }
    }

    public async Task<Result<HostMoveRequest?>> GetHostMoveRequestAsync(PhysicalAddress macAddress)
    {
        try
        {
            var hostMoveRequestsResult = await GetHostMoveRequestsAsync();

            if (hostMoveRequestsResult.IsFailed)
            {
                return Result.Fail<HostMoveRequest?>("Failed to retrieve host move requests.")
                             .WithErrors(hostMoveRequestsResult.Errors);
            }

            var request = hostMoveRequestsResult.Value.FirstOrDefault(r => r.MacAddress != null &&
                                                    r.MacAddress.GetAddressBytes()
                                                    .SequenceEqual(macAddress.GetAddressBytes()));
            return Result.Ok(request);
        }
        catch (Exception ex)
        {
            return Result.Fail<HostMoveRequest?>("Failed to retrieve the host move request.")
                         .WithError(ex.Message);
        }
    }

    public async Task<Result> AcknowledgeMoveRequestAsync(PhysicalAddress macAddress)
    {
        try
        {
            var hostMoveRequestsResult = await GetHostMoveRequestsAsync();

            if (hostMoveRequestsResult.IsFailed)
            {
                return Result.Fail("Failed to retrieve host move requests.")
                             .WithErrors(hostMoveRequestsResult.Errors);
            }

            var requestToRemove = hostMoveRequestsResult.Value.FirstOrDefault(r => r.MacAddress != null &&
                                                    r.MacAddress.GetAddressBytes()
                                                    .SequenceEqual(macAddress.GetAddressBytes()));

            if (requestToRemove == null)
            {
                return Result.Ok();
            }

            hostMoveRequestsResult.Value.Remove(requestToRemove);

            var saveResult = await SaveHostMoveRequestsAsync(hostMoveRequestsResult.Value);

            if (saveResult.IsFailed)
            {
                return Result.Fail("Failed to save updated host move requests.")
                             .WithErrors(saveResult.Errors);
            }

            await eventNotificationService.SendNotificationAsync($"Acknowledged move request for host with MAC address: {macAddress}");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("Failed to acknowledge move request.")
                         .WithError(ex.Message);
        }
    }
}
