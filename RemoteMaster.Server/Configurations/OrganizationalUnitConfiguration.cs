﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.Configurations;

public class OrganizationalUnitConfiguration : IEntityTypeConfiguration<OrganizationalUnit>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<OrganizationalUnit> builder)
    {
        builder.HasKey(ou => ou.Id);

        builder.Property(ou => ou.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(ou => ou.Name)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnOrder(1);

        builder.Property(ou => ou.ParentId)
            .HasColumnOrder(2);

        builder.Property(ou => ou.OrganizationId)
            .IsRequired()
            .HasColumnOrder(3);

        builder.HasIndex(ou => new { ou.Name, ou.OrganizationId })
            .IsUnique();

        builder.HasMany(ou => ou.Children)
            .WithOne(ou => ou.Parent)
            .HasForeignKey(ou => ou.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ou => ou.Organization)
            .WithMany(o => o.OrganizationalUnits)
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ou => ou.Hosts)
            .WithOne(h => h.Parent)
            .HasForeignKey(h => h.ParentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ou => ou.UserOrganizationalUnits)
            .WithOne(uou => uou.OrganizationalUnit)
            .HasForeignKey(uou => uou.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
