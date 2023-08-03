// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Client.Models;

public class Computer : Node
{
    public Computer()
    {
    }

    public Computer(string name, string ipAddress)
    {
        Name = name;
        IPAddress = ipAddress;
    }

    public string IPAddress { get; set; }
}
