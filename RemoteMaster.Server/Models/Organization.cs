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
    public Guid OrganizationId { get; set; }

    [NotMapped]
    public Guid NodeId
    {
        get => OrganizationId;
        set => OrganizationId = value;
    }

    public string Name { get; set; }

    public string OrganizationalUnit { get; set; }

    public string Locality { get; set; }

    public string State { get; set; }

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