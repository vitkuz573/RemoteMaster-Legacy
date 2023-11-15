using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Areas.Identity.Data;

public class IdentityDataContext : IdentityDbContext<IdentityUser>
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public IdentityDataContext(DbContextOptions<IdentityDataContext> options)
        : base(options)
    {
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId);
    }
}
