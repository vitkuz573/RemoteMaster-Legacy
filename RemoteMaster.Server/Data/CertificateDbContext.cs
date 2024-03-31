// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Data;

public class CertificateDbContext(DbContextOptions<CertificateDbContext> options) : DbContext(options)
{
    public DbSet<RevokedCertificate> RevokedCertificates { get; set; }

    public DbSet<CrlInfo> CrlInfos { get; set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RevokedCertificate>().ToTable("RevokedCertificates");
    }
}
