// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Client.Core.Abstractions;

public interface IScreenRecorderService
{
    Task StartRecordingAsync(string outputPath);

    Task StopRecordingAsync();
}
