using Blazorise;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteMaster.Client.Models;

public abstract class Node
{
    public Guid NodeId { get; set; } // Common Primary Key for all derived classes

    public string Name { get; set; }

    public Guid? ParentId { get; set; } // Parent Node's Id

    [ForeignKey(nameof(ParentId))]
    public Node Parent { get; set; }

    [InverseProperty(nameof(Node.Parent))]
    public ICollection<Node> Children { get; set; }

    public virtual IconName Icon { get; set; }

    public virtual IconName ExpandedIcon { get; set; }

    public string Type { get; set; } // to discriminate between Computer and Folder
}