// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Data;

public class CertificateDbContext(DbContextOptions<CertificateDbContext> options, IConfiguration? configuration = null) : DbContext(options)
{
    public DbSet<RevokedCertificate> RevokedCertificates { get; set; }

    public DbSet<CrlInfo> CrlInfos { get; set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "DbContextOptionsBuilder will not be null.")]
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (configuration != null)
        {
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), options =>
            {
                options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            optionsBuilder.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
        }
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<RevokedCertificate>()
            .ToTable("RevokedCertificates")
            .HasIndex(r => r.SerialNumber)
            .IsUnique();

        builder.Entity<RevokedCertificate>()
            .Property(r => r.Reason)
            .HasConversion<string>();
    }
}
