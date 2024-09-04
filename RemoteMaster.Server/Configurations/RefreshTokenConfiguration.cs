// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

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

        builder.OwnsOne(rt => rt.TokenValue, tokenValue =>
        {
            tokenValue.Property(t => t.Token)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnName("Token")
                .HasColumnOrder(2);

            tokenValue.Property(t => t.Expires)
                .IsRequired()
                .HasColumnName("Expires")
                .HasColumnOrder(3);

            tokenValue.Property(t => t.Created)
                .IsRequired()
                .HasColumnName("Created")
                .HasColumnOrder(4);

            tokenValue.Property(t => t.CreatedByIp)
                .IsRequired()
                .HasMaxLength(45)
                .HasColumnName("CreatedByIp")
                .HasColumnOrder(5);

            tokenValue.HasIndex(tv => tv.Expires);
        });

        builder.OwnsOne(rt => rt.RevocationInfo, revocationInfo =>
        {
            revocationInfo.Property(r => r.Revoked)
                .HasColumnName("Revoked")
                .HasColumnOrder(6);

            revocationInfo.Property(r => r.RevokedByIp)
                .HasMaxLength(45)
                .HasColumnName("RevokedByIp")
                .HasColumnOrder(7);

            revocationInfo.Property(r => r.RevocationReason)
                .HasConversion<string>()
                .HasColumnName("RevocationReason")
                .HasColumnOrder(8);

            revocationInfo.HasIndex(ri => ri.Revoked);
        });

        builder.HasIndex(rt => rt.UserId);

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);

        builder.HasOne(rt => rt.ReplacedByToken)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("RefreshTokens", t => t.HasCheckConstraint("CK_RefreshTokens_ReplacedByTokenId_Required", "[RevocationReason] <> 'Replaced' OR [ReplacedByTokenId] IS NOT NULL"));
    }
}
