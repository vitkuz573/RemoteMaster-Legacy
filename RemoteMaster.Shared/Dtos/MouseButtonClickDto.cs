using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class MouseButtonClickDto
{
    public long Button { get; set; }

    public ButtonAction State { get; set; }

    public int X { get; set; }

    public int Y { get; set; }
}
