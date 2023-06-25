using Blazorise;

namespace RemoteMaster.Client.Models;

public class Computer : Node
{
    public Computer()
    {
        Type = "computer";
    }

    public string IPAddress { get; set; }

    public override IconName Icon => IconName.Desktop;
}