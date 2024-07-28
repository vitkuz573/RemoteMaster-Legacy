// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasMany(u => u.AccessibleOrganizations)
            .WithMany(o => o.AccessibleUsers)
            .UsingEntity<Dictionary<string, object>>(
                "UserOrganizations",
                j => j.HasOne<Organization>()
                      .WithMany()
                      .HasForeignKey("OrganizationId")
                      .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey("UserId")
                      .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("OrganizationId", "UserId");
                    j.ToTable("UserOrganizations");
                    j.Property<Guid>("OrganizationId").HasColumnName("OrganizationId");
                    j.Property<string>("UserId").HasColumnName("UserId");
                });

        builder.HasMany(u => u.AccessibleOrganizationalUnits)
            .WithMany(ou => ou.AccessibleUsers)
            .UsingEntity<Dictionary<string, object>>(
                "UserOrganizationalUnits",
                j => j.HasOne<OrganizationalUnit>()
                      .WithMany()
                      .HasForeignKey("OrganizationalUnitId")
                      .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey("UserId")
                      .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("OrganizationalUnitId", "UserId");
                    j.ToTable("UserOrganizationalUnits");
                    j.Property<Guid>("OrganizationalUnitId").HasColumnName("OrganizationalUnitId");
                    j.Property<string>("UserId").HasColumnName("UserId");
                });

        builder.ToTable(tb => tb.HasTrigger("AspNetUsers_Trigger"));
    }
}
