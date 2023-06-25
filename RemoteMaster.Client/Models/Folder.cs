using Blazorise;

namespace RemoteMaster.Client.Models;

public class Folder : Node
{
    public Folder()
    {
        Type = "folder";
        Children = new List<Node>();
    }

    public IList<Node> Children { get; set; }

    public override IconName Icon => IconName.Folder;

    public override IconName ExpandedIcon => IconName.FolderOpen;
}