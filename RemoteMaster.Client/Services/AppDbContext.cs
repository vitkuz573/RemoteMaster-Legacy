// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Node> Nodes { get; set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Node>()
            .HasDiscriminator<string>("NodeType")
            .HasValue<Folder>("Folder")
            .HasValue<Computer>("Computer");

        modelBuilder.Entity<Node>()
            .HasMany(n => n.Children)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false);
    }
}


