// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.EventArguments;

public class SessionChangeEventArgs(nuint reason) : EventArgs
{
    public nuint Reason { get; } = reason;
}
