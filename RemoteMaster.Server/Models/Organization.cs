// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteMaster.Server.Models;

public class Organization
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid OrganizationId { get; set; }

    public string Name { get; set; }

    public string OrganizationalUnit { get; set; }

    public string Locality { get; set; }

    public string State { get; set; }

    public string Country { get; set; }

#pragma warning disable CA2227
    public virtual ICollection<UserOrganization> UserOrganizations { get; set; }
#pragma warning restore CA2227
}