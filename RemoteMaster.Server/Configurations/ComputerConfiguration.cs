// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Configurations;

public class ComputerConfiguration : IEntityTypeConfiguration<Computer>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<Computer> builder)
    {
        builder.HasKey(c => c.NodeId);

        builder.Property(c => c.NodeId)
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

        builder.Ignore(c => c.Thumbnail);

        builder.HasOne(c => (OrganizationalUnit?)c.Parent)
            .WithMany(ou => ou.Computers)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
