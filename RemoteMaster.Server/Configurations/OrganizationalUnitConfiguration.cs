// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Entities;

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
            .WithOne(c => (OrganizationalUnit?)c.Parent)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ou => ou.Organization)
            .WithMany(o => o.OrganizationalUnits)
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ou => ou.Computers)
            .WithOne(c => (OrganizationalUnit?)c.Parent)
            .HasForeignKey(c => c.ParentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ou => ou.AccessibleUsers)
            .WithMany(u => u.AccessibleOrganizationalUnits)
            .UsingEntity<Dictionary<string, object>>(
                "UserOrganizationalUnits",
                j => j.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey("UserId")
                      .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<OrganizationalUnit>()
                      .WithMany()
                      .HasForeignKey("OrganizationalUnitId")
                      .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("OrganizationalUnitId", "UserId");
                    j.ToTable("UserOrganizationalUnits");
                    j.Property<Guid>("OrganizationalUnitId").HasColumnName("OrganizationalUnitId");
                    j.Property<string>("UserId").HasColumnName("UserId");
                });
    }
}
