// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteMaster.Server.Models;

public class ApplicationClaim
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(Order = 0)]
    public int Id { get; set; }

    [Column(Order = 1)]
    public string ClaimType { get; set; } = string.Empty;

    [Column(Order = 2)]
    public string ClaimValue { get; set; } = string.Empty;
}
