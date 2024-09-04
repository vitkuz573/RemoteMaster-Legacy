// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.CrlAggregate;
using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Configurations;

public class CrlConfiguration : IEntityTypeConfiguration<Crl>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<Crl> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(c => c.Number)
            .IsRequired()
            .HasColumnOrder(1);
    }
}
