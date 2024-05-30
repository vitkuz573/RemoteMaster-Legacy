// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Organization> Organizations { get; set; }

    public DbSet<OrganizationalUnit> OrganizationalUnits { get; set; }

    public DbSet<Computer> Computers { get; set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Organization>()
            .Property(p => p.OrganizationalUnit)
            .HasDefaultValue("Default");

        builder.Entity<RefreshToken>()
            .Property(r => r.RevocationReason)
            .HasConversion<string>();

        builder.Entity<RefreshToken>()
            .HasIndex(p => p.UserId);

        builder.Entity<RefreshToken>()
            .HasIndex(p => p.Expires);

        builder.Entity<RefreshToken>()
            .HasIndex(p => p.Revoked);

        builder.Entity<OrganizationalUnit>()
            .HasKey(ou => ou.NodeId);

        builder.Entity<OrganizationalUnit>()
            .HasMany(ou => ou.Children)
            .WithOne(c => (OrganizationalUnit?)c.Parent)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false);

        builder.Entity<OrganizationalUnit>()
            .HasOne(ou => ou.Organization)
            .WithMany(o => o.OrganizationalUnits)
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OrganizationalUnit>()
            .HasMany(ou => ou.Computers)
            .WithOne(c => (OrganizationalUnit?)c.Parent)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false);

        builder.Entity<ApplicationUser>()
            .HasMany(u => u.AccessibleOrganizations)
            .WithMany(o => o.AccessibleUsers)
            .UsingEntity<Dictionary<string, object>>(
                "UserOrganizations",
                j => j.HasOne<Organization>().WithMany().HasForeignKey("OrganizationId"),
                j => j.HasOne<ApplicationUser>().WithMany().HasForeignKey("UserId"),
                j =>
                {
                    j.HasKey("OrganizationId", "UserId");
                    j.ToTable("UserOrganizations");
                    j.Property<Guid>("OrganizationId").HasColumnName("OrganizationId");
                    j.Property<string>("UserId").HasColumnName("UserId");
                });

        builder.Entity<ApplicationUser>()
            .HasMany(u => u.AccessibleOrganizationalUnits)
            .WithMany(ou => ou.AccessibleUsers)
            .UsingEntity<Dictionary<string, object>>(
                "UserOrganizationalUnits",
                j => j.HasOne<OrganizationalUnit>().WithMany().HasForeignKey("OrganizationalUnitId"),
                j => j.HasOne<ApplicationUser>().WithMany().HasForeignKey("UserId"),
                j =>
                {
                    j.HasKey("OrganizationalUnitId", "UserId");
                    j.ToTable("UserOrganizationalUnits");
                    j.Property<Guid>("OrganizationalUnitId").HasColumnName("OrganizationalUnitId");
                    j.Property<string>("UserId").HasColumnName("UserId");
                });
    }
}
