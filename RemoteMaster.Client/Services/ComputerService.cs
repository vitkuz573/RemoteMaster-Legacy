using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Services;

public class ComputerService
{
    private List<Folder> folders = new List<Folder>
    {
        new Folder
        {
            Name = "Folder 1",
            Children = new List<Node>
            {
                new Computer { Name = "Computer 1", IPAddress = "192.168.0.1" },
                new Computer { Name = "Computer 2", IPAddress = "192.168.0.2" },
                new Computer { Name = "Computer 3", IPAddress = "192.168.0.3" }
            }
        },
        // Other folders...
    };

    public List<Folder> GetFolders()
    {
        return folders;
    }

    public Computer GetComputerByIp(string ipAddress)
    {
        foreach (var folder in folders)
        {
            foreach (var child in folder.Children)
            {
                if (child is Computer computer && computer.IPAddress == ipAddress)
                {
                    return computer;
                }
            }
        }

        return null;
    }
}
