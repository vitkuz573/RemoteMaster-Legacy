// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Data;

public class NodesDbContext(DbContextOptions<NodesDbContext> options) : DbContext(options)
{
    public DbSet<OrganizationalUnit> OrganizationalUnits { get; set; }

    public DbSet<Computer> Computers { get; set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationalUnit>()
            .HasKey(ou => ou.NodeId);

        modelBuilder.Entity<OrganizationalUnit>()
            .HasMany(ou => ou.Children)
            .WithOne(c => (OrganizationalUnit?)c.Parent)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false);

        modelBuilder.Entity<OrganizationalUnit>()
            .HasOne(ou => ou.Organization)
            .WithMany(o => o.OrganizationalUnits)
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrganizationalUnit>()
            .HasMany(ou => ou.Computers)
            .WithOne(c => (OrganizationalUnit?)c.Parent)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false);

        modelBuilder.Ignore<UserOrganization>();
        modelBuilder.Ignore<UserOrganizationalUnit>();
        modelBuilder.Ignore<ApplicationUser>();
        modelBuilder.Ignore<Organization>();
    }
}

