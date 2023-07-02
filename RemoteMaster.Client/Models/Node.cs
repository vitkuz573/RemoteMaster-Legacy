using Blazorise;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteMaster.Client.Models;

public abstract class Node
{
    public Guid NodeId { get; set; }

    public string Name { get; set; }

    public Guid? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public Node Parent { get; set; }

    [InverseProperty(nameof(Parent))]
    public ICollection<Node> Children { get; set; }

    [NotMapped]
    public virtual IconName Icon { get; set; }

    [NotMapped]
    public virtual IconName ExpandedIcon { get; set; }
}