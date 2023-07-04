using Blazorise;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteMaster.Client.Models;

public class Computer : Node
{
    public Computer()
    {
    }

    public Computer(string name, string ipAddress)
    {
        Name = name;
        IPAddress = ipAddress;
    }

    public string IPAddress { get; set; }

    [NotMapped]
    public override IconName Icon => IconName.Desktop;
}
