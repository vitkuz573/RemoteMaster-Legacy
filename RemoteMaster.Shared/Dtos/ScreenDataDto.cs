namespace RemoteMaster.Shared.Dtos;

public class ScreenDataDto
{
    public IEnumerable<string> DisplayNames { get; init; } = Enumerable.Empty<string>();

    public string SelectedDisplay { get; init; } = string.Empty;

    public int ScreenWidth { get; init; }

    public int ScreenHeight { get; init; }
}
