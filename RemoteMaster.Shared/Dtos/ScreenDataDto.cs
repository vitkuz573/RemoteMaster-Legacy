using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class ScreenDataDto
{
    public IEnumerable<DisplayInfo> Displays { get; init; } = Enumerable.Empty<DisplayInfo>();

    public int ScreenWidth { get; init; }

    public int ScreenHeight { get; init; }
}
