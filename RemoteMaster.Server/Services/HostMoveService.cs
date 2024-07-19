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

    public async Task<List<HostMoveRequest>> GetHostMoveRequestsAsync()
    {
        if (!File.Exists(HostMoveRequestsFilePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(HostMoveRequestsFilePath);

        return JsonSerializer.Deserialize<List<HostMoveRequest>>(json) ?? [];

    }

    public async Task SaveHostMoveRequestsAsync(List<HostMoveRequest> hostMoveRequests)
    {
        var updatedJson = JsonSerializer.Serialize(hostMoveRequests);

        await File.WriteAllTextAsync(HostMoveRequestsFilePath, updatedJson);
    }

    public async Task<HostMoveRequest?> GetHostMoveRequestAsync(string macAddress)
    {
        var hostMoveRequests = await GetHostMoveRequestsAsync();
        
        return hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AcknowledgeMoveRequestAsync(string macAddress)
    {
        var hostMoveRequests = await GetHostMoveRequestsAsync();
        var requestToRemove = hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

        if (requestToRemove != null)
        {
            hostMoveRequests.Remove(requestToRemove);
            
            await SaveHostMoveRequestsAsync(hostMoveRequests);
            await eventNotificationService.SendNotificationAsync($"Acknowledged move request for host with MAC address: {macAddress}");
        }
    }
}
