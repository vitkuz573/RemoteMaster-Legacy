﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RemoteMaster.Server.Aggregates.AuditLogAggregate;
using RemoteMaster.Server.Configurations;

namespace RemoteMaster.Server.Data;

public class AuditLogDbContext(DbContextOptions<AuditLogDbContext> options, IConfiguration? configuration = null) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs { get; set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "DbContextOptionsBuilder will not be null.")]
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (configuration == null)
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("AuditLogConnection");

        optionsBuilder.UseSqlite(connectionString, options =>
        {
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

        optionsBuilder.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
    }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ModelBuilder will not be null.")]
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new AuditLogConfiguration());
    }
}
