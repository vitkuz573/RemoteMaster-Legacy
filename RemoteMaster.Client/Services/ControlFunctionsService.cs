namespace RemoteMaster.Client.Services;

public class ControlFunctionsService
{
    public Action KillServer { get; set; }

    public Action RebootComputer { get; set; }

    public string[] Displays { get; set; }

    public Action<string> SelectDisplay { get; set; }
}
