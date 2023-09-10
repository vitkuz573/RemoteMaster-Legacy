using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Hubs;

public class ManagementHub : Hub
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<ManagementHub> _logger;

    public ManagementHub(DatabaseService databaseService, ILogger<ManagementHub> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<bool> RegisterClient(string hostName, string ipAddress, string group)
    {
        var folder = _databaseService.GetFolders().FirstOrDefault(f => f.Name == group);

        if (folder == null)
        {
            folder = new Folder(group);
            _databaseService.AddNode(folder);
        }

        var existingComputer = _databaseService.GetComputersByFolderId(folder.NodeId).FirstOrDefault(c => c.Name == hostName);

        if (existingComputer != null)
        {
            existingComputer.IPAddress = ipAddress;
        }
        else
        {
            var computer = new Computer
            {
                Name = hostName,
                IPAddress = ipAddress,
                Parent = folder
            };

            _databaseService.AddNode(computer);
        }

        return true;
    }

    public async Task<bool> UnregisterClient(string hostName, string group)
    {
        var folder = _databaseService.GetFolders().FirstOrDefault(f => f.Name == group);

        if (folder == null)
        {
            _logger.LogWarning($"Unregistration failed: Folder '{group}' not found.");

            return false;
        }

        var existingComputer = _databaseService.GetComputerByNameAndFolderId(hostName, folder.NodeId);

        if (existingComputer != null)
        {
            _databaseService.RemoveNode(existingComputer);

            var remainingComputers = _databaseService.GetComputersByFolderId(folder.NodeId);

            if (!remainingComputers.Any())
            {
                _databaseService.RemoveNode(folder);
            }

            return true;
        }

        _logger.LogWarning($"Unregistration failed: Computer '{hostName}' not found in folder '{group}'.");

        return false;
    }
}