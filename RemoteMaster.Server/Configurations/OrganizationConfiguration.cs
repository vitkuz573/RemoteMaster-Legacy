// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.NodeId);

        builder.Property(o => o.NodeId)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasColumnOrder(1);

        builder.Property(o => o.Locality)
            .IsRequired()
            .HasColumnOrder(2);

        builder.Property(o => o.State)
            .IsRequired()
            .HasColumnOrder(3);

        builder.Property(o => o.Country)
            .IsRequired()
            .HasColumnOrder(4);

        builder.HasIndex(o => o.Name)
            .IsUnique();

        builder.Ignore(o => o.ParentId);
        builder.Ignore(o => o.Parent);
    }
}
