using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class KeyboardKeyDto
{
    public int Key { get; set; }

    public ButtonAction State { get; set; }
}
