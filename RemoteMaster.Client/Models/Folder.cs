// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

namespace RemoteMaster.Client.Models;

public class Folder : Node
{
    public Folder()
    {
        Children = new List<Node>();
    }

    public Folder(string name)
    {
        Name = name;
        Children = new List<Node>();
    }
}


