namespace RemoteMaster.Shared.Models;

public class WindowsSession
{
    public uint Id { get; set; }

    public string Name { get; set; }

    public SessionType Type { get; set; }

    public string Username { get; set; }
}

public enum SessionType
{
    Console,
    RDP,
}