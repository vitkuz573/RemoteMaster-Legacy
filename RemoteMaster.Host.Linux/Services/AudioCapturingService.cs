// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class AudioCapturingService : IAudioCapturingService
{
    public void StartRecording() { }

    public void StopRecording() { }

    public byte[]? GetNextAudioChunk()
    {
        return [];
    }

    public void Dispose() { }
}
