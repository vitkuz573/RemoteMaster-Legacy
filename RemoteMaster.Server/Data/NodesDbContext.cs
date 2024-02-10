// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Data;

public class NodesDbContext(DbContextOptions<NodesDbContext> options) : DbContext(options)
{
    public DbSet<Node> Nodes { get; set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Node>()
            .HasDiscriminator<string>("NodeType")
            .HasValue<OrganizationalUnit>("OrganizationalUnit")
            .HasValue<Computer>("Computer");

        modelBuilder.Entity<Node>()
            .HasMany(n => n.Nodes)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false);
    }
}


