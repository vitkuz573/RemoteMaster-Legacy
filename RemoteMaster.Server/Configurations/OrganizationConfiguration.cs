// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

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

        builder.OwnsOne(o => o.Address, address =>
        {
            address.Property(a => a.Locality)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Locality")
                .HasColumnOrder(2);

            address.Property(a => a.State)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("State")
                .HasColumnOrder(3);

            address.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Country")
                .HasColumnOrder(4);

            address.HasIndex(a => new { a.Locality, a.State, a.Country });
        });

        builder.HasIndex(o => o.Name)
            .IsUnique();

        builder.HasMany(o => o.OrganizationalUnits)
            .WithOne(ou => ou.Organization)
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.UserOrganizations)
            .WithOne(uo => uo.Organization)
            .HasForeignKey(uo => uo.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
