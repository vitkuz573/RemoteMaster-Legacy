// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

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
