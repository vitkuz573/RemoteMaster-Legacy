// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Host = RemoteMaster.Server.Aggregates.OrganizationAggregate.Host;

namespace RemoteMaster.Server.Configurations;

public class HostConfiguration : IEntityTypeConfiguration<Host>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<Host> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnOrder(1);

        builder.Property(c => c.IpAddress)
            .IsRequired()
            .HasMaxLength(45)
            .HasColumnOrder(2);

        builder.Property(c => c.MacAddress)
            .IsRequired()
            .HasMaxLength(17)
            .HasColumnOrder(3);

        builder.HasOne(c => c.Parent)
            .WithMany(ou => ou.Hosts)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.MacAddress)
            .IsUnique();
    }
}
