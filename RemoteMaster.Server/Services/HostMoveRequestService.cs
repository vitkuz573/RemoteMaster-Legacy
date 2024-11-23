// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Net.NetworkInformation;
using System.Text.Json;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.JsonContexts;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostMoveRequestService : IHostMoveRequestService
{
    private readonly IEventNotificationService _eventNotificationService;
    private readonly IFileSystem _fileSystem;

    private readonly string _hostMoveRequestsFilePath;

    public HostMoveRequestService(IEventNotificationService eventNotificationService, IFileSystem fileSystem)
    {
        _eventNotificationService = eventNotificationService;
        _fileSystem = fileSystem;
        
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        
        _hostMoveRequestsFilePath = _fileSystem.Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");
    }

    public async Task<Result<List<HostMoveRequest>>> GetHostMoveRequestsAsync()
    {
        try
        {
            if (!_fileSystem.File.Exists(_hostMoveRequestsFilePath))
            {
                return Result.Ok(new List<HostMoveRequest>());
            }

            var json = await _fileSystem.File.ReadAllTextAsync(_hostMoveRequestsFilePath);
            var hostMoveRequests = JsonSerializer.Deserialize(json, HostJsonSerializerContext.Default.ListHostMoveRequest) ?? [];

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
            var updatedJson = JsonSerializer.Serialize(hostMoveRequests, HostJsonSerializerContext.Default.ListHostMoveRequest);
            await _fileSystem.File.WriteAllTextAsync(_hostMoveRequestsFilePath, updatedJson);

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
            var hostMoveRequestsResult = await GetHostMoveRequestsAsync();

            if (hostMoveRequestsResult.IsFailed)
            {
                return Result.Fail("Failed to retrieve host move requests.")
                             .WithErrors(hostMoveRequestsResult.Errors);
            }

            var requestToRemove = hostMoveRequestsResult.Value.FirstOrDefault(r => r.MacAddress.Equals(macAddress));

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

            await _eventNotificationService.SendNotificationAsync($"Acknowledged move request for host with MAC address: {macAddress}");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("Failed to acknowledge move request.")
                         .WithError(ex.Message);
        }
    }
}
