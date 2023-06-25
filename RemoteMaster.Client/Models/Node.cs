using Blazorise;

namespace RemoteMaster.Client.Models;

public abstract class Node
{
    public string Name { get; set; }

    public string Type { get; set; }

    public Folder Parent { get; set; }

    public virtual IconName Icon { get; }

    public virtual IconName ExpandedIcon { get; }
}