using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Node> Nodes { get; set; }

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


