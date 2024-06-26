// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class NativeProcessFactory : INativeProcessFactory
{
    public INativeProcess Create()
    {
        return new NativeProcess();
    }
}