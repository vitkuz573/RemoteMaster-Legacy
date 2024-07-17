// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RemoteMaster.Server.Configurations;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IConfiguration? _configuration;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Organization> Organizations { get; set; }

    public DbSet<OrganizationalUnit> OrganizationalUnits { get; set; }

    public DbSet<Computer> Computers { get; set; }

    public DbSet<SignInEntry> SignInEntries { get; set; }

    public DbSet<ApplicationClaim> ApplicationClaims { get; set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "DbContextOptionsBuilder will not be null.")]
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_configuration == null)
        {
            return;
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

        optionsBuilder.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new OrganizationConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        builder.ApplyConfiguration(new OrganizationalUnitConfiguration());
        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new SignInEntryConfiguration());
    }
}
