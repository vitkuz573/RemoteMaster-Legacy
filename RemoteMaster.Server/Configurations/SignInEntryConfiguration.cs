// Copyright © 2023 Vitaly Kuzyaев. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Configurations;

public class SignInEntryConfiguration : IEntityTypeConfiguration<SignInEntry>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<SignInEntry> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.UserId);

        builder.Property(s => s.SignInTime)
            .IsRequired();

        builder.Property(s => s.IsSuccessful)
            .IsRequired();

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45)
            .IsRequired();
    }
}
