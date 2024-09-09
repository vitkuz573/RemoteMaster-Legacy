// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Host.Core.Entities;

namespace RemoteMaster.Host.Core.Configurations;

internal class ConfigurationSyncStateConfiguration : IEntityTypeConfiguration<ConfigurationSyncState>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<ConfigurationSyncState> builder)
    {
        builder.HasKey(css => css.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(css => css.IsSyncRequired)
            .IsRequired()
            .HasColumnOrder(1);

        builder.Property(css => css.LastAttempt)
            .HasColumnOrder(2);
    }
}
