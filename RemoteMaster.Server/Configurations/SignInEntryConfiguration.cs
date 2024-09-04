// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Configurations;

public class SignInEntryConfiguration : IEntityTypeConfiguration<SignInEntry>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<SignInEntry> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.HasIndex(s => s.UserId);

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasColumnOrder(1);

        builder.Property(s => s.SignInTime)
            .IsRequired()
            .HasColumnOrder(2);

        builder.Property(s => s.IsSuccessful)
            .IsRequired()
            .HasColumnOrder(3);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45)
            .IsRequired()
            .HasColumnOrder(4);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
