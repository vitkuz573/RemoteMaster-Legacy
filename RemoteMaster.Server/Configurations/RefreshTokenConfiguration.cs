// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(rt => rt.UserId)
            .IsRequired()
            .HasMaxLength(450)
            .HasColumnOrder(1);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnOrder(2);

        builder.Property(rt => rt.Expires)
            .IsRequired()
            .HasColumnOrder(3);

        builder.Property(rt => rt.Created)
            .IsRequired()
            .HasColumnOrder(4);

        builder.Property(rt => rt.CreatedByIp)
            .IsRequired()
            .HasMaxLength(45)
            .HasColumnOrder(5);

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(45)
            .HasColumnOrder(6);

        builder.Property(rt => rt.RevocationReason)
            .HasConversion<string>()
            .IsRequired()
            .HasDefaultValue(TokenRevocationReason.None)
            .HasColumnOrder(7);

        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.Expires);
        builder.HasIndex(rt => rt.Revoked);

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);

        builder.HasOne(rt => rt.ReplacedByToken)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("RefreshTokens", t => t.HasCheckConstraint("CK_RefreshTokens_ReplacedByTokenId_Required", "[RevocationReason] <> 'Replaced' OR [ReplacedByTokenId] IS NOT NULL"));
    }
}
