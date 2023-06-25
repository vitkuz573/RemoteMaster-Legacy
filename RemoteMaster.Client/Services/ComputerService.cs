using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Services;

public class ComputerService
{
    private List<Computer> computers = new List<Computer>
    {
        new Computer { Name = "Computer 1", IPAddress = "192.168.0.1" },
        new Computer { Name = "Computer 2", IPAddress = "192.168.0.2" },
        new Computer { Name = "Computer 3", IPAddress = "192.168.0.3" }
    };

    public Computer GetComputerByIp(string ipAddress)
    {
        return computers.FirstOrDefault(c => c.IPAddress == ipAddress);
    }
}
