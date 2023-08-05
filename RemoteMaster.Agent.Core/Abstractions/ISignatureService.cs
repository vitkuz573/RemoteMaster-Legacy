// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;

namespace RemoteMaster.Agent.Core.Abstractions;

public interface ISignatureService
{
    bool IsSignatureValid(string filePath, string expectedThumbprint);

    bool IsProcessSignatureValid(Process process, string expectedPath, string expectedThumbprint);
}
