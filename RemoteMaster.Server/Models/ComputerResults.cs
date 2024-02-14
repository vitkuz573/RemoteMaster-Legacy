// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;

namespace RemoteMaster.Server.Models;

public class ComputerResults
{
    public StringBuilder Messages { get; } = new();

    public int? LastPid { get; set; }

    public override string ToString() => Messages.ToString();
}