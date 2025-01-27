// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.AuditLogAggregate;

namespace RemoteMaster.Server.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(a => a.ActionType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(1);

        builder.Property(a => a.UserName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnOrder(2);

        builder.Property(a => a.ActionTime)
            .IsRequired()
            .HasColumnOrder(3);

        builder.Property(a => a.Details)
            .IsRequired()
            .HasMaxLength(1000)
            .HasColumnOrder(4);
    }
}
