using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Client.Services;

public class ControlFunctionsService
{
    public Action KillServer { get; set; }

    public Action RebootComputer { get; set; }

    public IEnumerable<(string, bool)> Displays { get; set; }

    public Action<SelectScreenDto> SelectDisplay { get; set; }
}
