using Blazorise;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteMaster.Client.Maui.Models;

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

    [NotMapped]
    public override IconName Icon => IconName.Folder;

    [NotMapped]
    public override IconName ExpandedIcon => IconName.FolderOpen;
}


