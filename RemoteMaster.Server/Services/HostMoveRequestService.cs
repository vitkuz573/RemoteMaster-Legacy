// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.HostMoveRequestAggregate;

namespace RemoteMaster.Server.Services;

public class HostMoveRequestService(IEventNotificationService eventNotificationService, IHostMoveRequestUnitOfWork hostMoveRequestUnitOfWork) : IHostMoveRequestService
{
    public async Task<Result<List<HostMoveRequest>>> GetHostMoveRequestsAsync()
    {
        try
        {
            var hostMoveRequests = (await hostMoveRequestUnitOfWork.HostMoveRequests.GetAllAsync()).ToList();

            return Result.Ok(hostMoveRequests);
        }
        catch (Exception ex)
        {
            return Result.Fail<List<HostMoveRequest>>("Failed to retrieve host move requests.")
                         .WithError(ex.Message);
        }
    }

    public async Task<Result> SaveHostMoveRequestsAsync()
    {
        try
        {
            await hostMoveRequestUnitOfWork.CommitAsync();

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

            var request = hostMoveRequestsResult.Value.FirstOrDefault(r => r.MacAddress.Equals(macAddress));

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
            var hostMoveRequestToRemove = (await hostMoveRequestUnitOfWork.HostMoveRequests.FindAsync(hmr => hmr.MacAddress.Equals(macAddress))).First();

            hostMoveRequestUnitOfWork.HostMoveRequests.Delete(hostMoveRequestToRemove);

            var saveResult = await SaveHostMoveRequestsAsync();

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
