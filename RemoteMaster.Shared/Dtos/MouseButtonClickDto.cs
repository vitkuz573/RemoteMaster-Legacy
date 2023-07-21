using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class MouseButtonClickDto
{
    public long Button { get; set; }

    public ButtonAction State { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double ImgWidth { get; init; }

    public double ImgHeight { get; init; }
}
