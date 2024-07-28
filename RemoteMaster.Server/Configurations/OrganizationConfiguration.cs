// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnOrder(1);

        builder.Property(o => o.Locality)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(2);

        builder.Property(o => o.State)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(3);

        builder.Property(o => o.Country)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnOrder(4);

        builder.HasIndex(o => o.Name)
            .IsUnique();

        builder.HasMany(o => o.OrganizationalUnits)
            .WithOne(ou => ou.Organization)
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(o => o.ParentId);
        builder.Ignore(o => o.Parent);
    }
}
