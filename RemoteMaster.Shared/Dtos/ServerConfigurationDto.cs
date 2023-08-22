// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Dtos;

public class ServerConfigurationDto
{
    public bool InputEnabled { get; init; }

    public bool TrackCursor { get; init; }

    public int ImageQuality { get; init; }

    public override string ToString()
    {
        return $"ServerConfigurationDto: {{ InputEnabled: {InputEnabled}, TrackCursor: {TrackCursor}, ImageQuality: {ImageQuality} }}";
    }
}
