// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RemoteMaster.Server.Data;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Models;

public class Organization : INode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(Order = 0)]
    public Guid NodeId { get; set; }

    [Required]
    [Column(Order = 1)]
    public string Name { get; set; }

    [Required]
    [Column(Order = 2)]
    public string Locality { get; set; }

    [Required]
    [Column(Order = 3)]
    public string State { get; set; }

    [Required]
    [Column(Order = 4)]
    public string Country { get; set; }

    [NotMapped]
    public Guid? ParentId { get; set; }

    [NotMapped]
    public INode? Parent { get; set; }

#pragma warning disable CA2227
    public ICollection<OrganizationalUnit> OrganizationalUnits { get; set; }

    public ICollection<ApplicationUser> AccessibleUsers { get; set; }
#pragma warning restore CA2227
}