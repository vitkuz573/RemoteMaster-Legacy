// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.Configurations;

public class UserOrganizationalUnitConfiguration : IEntityTypeConfiguration<UserOrganizationalUnit>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<UserOrganizationalUnit> builder)
    {
        builder.HasKey(uou => new { uou.OrganizationalUnitId, uou.UserId });

        builder.HasOne(uou => uou.OrganizationalUnit)
            .WithMany(ou => ou.UserOrganizationalUnits)
            .HasForeignKey(uou => uou.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("UserOrganizationalUnits");
    }
}