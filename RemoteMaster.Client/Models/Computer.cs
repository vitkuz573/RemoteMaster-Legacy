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
}
