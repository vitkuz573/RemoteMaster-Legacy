using System.Drawing;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Dtos;

public class ScreenDataDto
{
    public IEnumerable<DisplayInfo> Displays { get; init; } = Enumerable.Empty<DisplayInfo>();

    public Size ScreenSize { get; set; }
}
