using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Hubs;

public class ManagementHub : Hub
{
    private readonly DatabaseService _databaseService;

    public ManagementHub(DatabaseService databaseService)
    {
        _databaseService = databaseService;
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
}
