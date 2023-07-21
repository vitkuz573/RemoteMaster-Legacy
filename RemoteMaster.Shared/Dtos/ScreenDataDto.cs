namespace RemoteMaster.Shared.Dtos;

public class ScreenDataDto
{
    public IEnumerable<(string, bool)> Displays { get; init; } = Enumerable.Empty<(string, bool)>();

    public string SelectedDisplay { get; init; } = string.Empty;

    public int ScreenWidth { get; init; }

    public int ScreenHeight { get; init; }
}
