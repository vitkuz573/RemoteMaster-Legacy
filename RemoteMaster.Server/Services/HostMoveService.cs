// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
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
                return Result<List<HostMoveRequest>>.Success([]);
            }

            var json = await File.ReadAllTextAsync(HostMoveRequestsFilePath);
            var hostMoveRequests = JsonSerializer.Deserialize<List<HostMoveRequest>>(json) ?? [];

            return Result<List<HostMoveRequest>>.Success(hostMoveRequests);
        }
        catch (Exception ex)
        {
            return Result<List<HostMoveRequest>>.Failure("Failed to retrieve host move requests.", exception: ex);
        }
    }

    public async Task<Result> SaveHostMoveRequestsAsync(List<HostMoveRequest> hostMoveRequests)
    {
        try
        {
            var updatedJson = JsonSerializer.Serialize(hostMoveRequests);
            await File.WriteAllTextAsync(HostMoveRequestsFilePath, updatedJson);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("Failed to save host move requests.", exception: ex);
        }
    }

    public async Task<Result<HostMoveRequest?>> GetHostMoveRequestAsync(string macAddress)
    {
        try
        {
            var hostMoveRequests = await GetHostMoveRequestsAsync();

            if (!hostMoveRequests.IsSuccess)
            {
                return Result<HostMoveRequest?>.Failure([.. hostMoveRequests.Errors]);
            }

            var request = hostMoveRequests.Value.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
            
            return Result<HostMoveRequest?>.Success(request);
        }
        catch (Exception ex)
        {
            return Result<HostMoveRequest?>.Failure("Failed to retrieve the host move request.", exception: ex);
        }
    }

    public async Task<Result> AcknowledgeMoveRequestAsync(string macAddress)
    {
        try
        {
            var hostMoveRequests = await GetHostMoveRequestsAsync();

            if (!hostMoveRequests.IsSuccess)
            {
                return Result.Failure([.. hostMoveRequests.Errors]);
            }

            var requestToRemove = hostMoveRequests.Value.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

            if (requestToRemove == null)
            {
                return Result.Success();
            }

            hostMoveRequests.Value.Remove(requestToRemove);

            var saveResult = await SaveHostMoveRequestsAsync(hostMoveRequests.Value);

            if (!saveResult.IsSuccess)
            {
                return Result.Failure([.. saveResult.Errors]);
            }

            await eventNotificationService.SendNotificationAsync($"Acknowledged move request for host with MAC address: {macAddress}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("Failed to acknowledge move request.", exception: ex);
        }
    }
}
