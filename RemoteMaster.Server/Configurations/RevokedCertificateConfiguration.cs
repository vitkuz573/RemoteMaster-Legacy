// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Configurations;

public class RevokedCertificateConfiguration : IEntityTypeConfiguration<RevokedCertificate>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<RevokedCertificate> builder)
    {
        builder.HasKey(rc => rc.Id);

        builder.Property(rc => rc.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(rc => rc.SerialNumber)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnOrder(1);

        builder.Property(rc => rc.Reason)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnOrder(2);

        builder.Property(rc => rc.RevocationDate)
            .IsRequired()
            .HasColumnOrder(3);

        builder.HasIndex(rc => rc.SerialNumber)
            .IsUnique();

        builder.ToTable("RevokedCertificates");
    }
}