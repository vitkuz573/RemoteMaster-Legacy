// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RemoteMaster.Server.Configurations;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Data;

public class CertificateDbContext : DbContext
{
    private readonly IConfiguration? _configuration;

    public CertificateDbContext(DbContextOptions<CertificateDbContext> options) : base(options)
    {
    }

    public CertificateDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbSet<RevokedCertificate> RevokedCertificates { get; set; }

    public DbSet<CrlInfo> CrlInfos { get; set; }

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

        builder.ApplyConfiguration(new RevokedCertificateConfiguration());
    }
}
