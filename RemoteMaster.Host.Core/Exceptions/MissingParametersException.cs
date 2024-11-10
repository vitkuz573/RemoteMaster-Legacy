// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Exceptions;

public class MissingParametersException(string launchModeName, List<KeyValuePair<string, ILaunchParameter>> missingParameters) : Exception($"Missing required parameters for launch mode: {launchModeName}.")
{
    public string LaunchModeName { get; } = launchModeName;
    
    public List<KeyValuePair<string, ILaunchParameter>> MissingParameters { get; } = missingParameters;
}
