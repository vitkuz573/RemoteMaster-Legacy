// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Dtos;

public class ScreenRecordingDto(string output)
{
    public string Output { get; } = output;

    public uint Duration { get; init; }

    public uint VideoQuality { get; init; }
}
