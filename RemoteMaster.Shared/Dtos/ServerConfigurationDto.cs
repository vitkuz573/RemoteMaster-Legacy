using System.Text;

namespace RemoteMaster.Shared.Dtos;

public class ServerConfigurationDto
{
    public bool InputEnabled { get; init; }

    public bool TrackCursor { get; init; }

    public int ImageQuality { get; init; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("Server Configuration:");
        stringBuilder.AppendLine("---------------------");
        stringBuilder.AppendLine($"Input Enabled: {InputEnabled,-5}\tTrack Cursor: {TrackCursor,-5}");
        stringBuilder.AppendLine($"Image Quality: {ImageQuality}%\t(Quality: {GetQualityLabel()})");

        return stringBuilder.ToString();

        string GetQualityLabel()
        {
            return ImageQuality <= 25 ? "Low" : ImageQuality <= 50 ? "Medium" : ImageQuality <= 75 ? "High" : "Ultra";
        }
    }
}
