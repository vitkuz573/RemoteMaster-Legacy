// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RemoteMaster.Server.Data;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Models;

public class OrganizationalUnit : INode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(Order = 0)]
    public Guid NodeId { get; set; }

    [Required]
    [Column(Order = 1)]
    public string Name { get; set; }

    [Column(Order = 2)]
    public Guid? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public INode? Parent { get; set; }

    [Required]
    [Column(Order = 3)]
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; }

#pragma warning disable CA2227
    public ICollection<OrganizationalUnit> Children { get; set; }

    public ICollection<Computer> Computers { get; set; }

    public ICollection<ApplicationUser> AccessibleUsers { get; set; }
#pragma warning restore CA2227
}
