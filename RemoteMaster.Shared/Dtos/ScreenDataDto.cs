using System.Drawing;

namespace RemoteMaster.Shared.Dtos;

public class ScreenDataDto
{
    public IEnumerable<(string, bool, Size)> Displays { get; init; } = Enumerable.Empty<(string, bool, Size)>();

    public int ScreenWidth { get; init; }

    public int ScreenHeight { get; init; }
}
