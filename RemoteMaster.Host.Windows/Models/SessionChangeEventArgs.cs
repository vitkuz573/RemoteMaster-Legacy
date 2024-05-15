// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Windows.Models;

public class SessionChangeEventArgs(string changeDescription) : EventArgs
{
    public string ChangeDescription { get; } = changeDescription;
}

