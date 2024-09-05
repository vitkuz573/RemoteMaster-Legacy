// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.ApplicationClaimAggregate;

namespace RemoteMaster.Server.Configurations;

public class ApplicationClaimConfiguration : IEntityTypeConfiguration<ApplicationClaim>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<ApplicationClaim> builder)
    {
        builder.HasKey(ac => ac.Id);

        builder.Property(ac => ac.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(ac => ac.ClaimType)
            .IsRequired()
            .HasColumnOrder(1);

        builder.Property(ac => ac.ClaimValue)
            .IsRequired()
            .HasColumnOrder(2);

        builder.Property(ac => ac.DisplayName)
            .IsRequired()
            .HasColumnOrder(3);

        builder.Property(ac => ac.Description)
            .IsRequired()
            .HasColumnOrder(4);
    }
}