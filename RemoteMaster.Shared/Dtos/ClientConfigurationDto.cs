// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;

namespace RemoteMaster.Shared.Dtos;

public class ClientConfigurationDto
{
    public bool InputEnabled { get; init; }

    public bool TrackCursor { get; init; }

    public int ImageQuality { get; init; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("Client Configuration:");
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
