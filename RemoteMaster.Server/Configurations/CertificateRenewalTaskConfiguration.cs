// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.Configurations;

public class CertificateRenewalTaskConfiguration : IEntityTypeConfiguration<CertificateRenewalTask>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<CertificateRenewalTask> builder)
    {
        builder.HasKey(crt => crt.Id);

        builder.Property(crt => crt.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(crt => crt.ComputerId)
            .IsRequired()
            .HasColumnOrder(1);

        builder.HasOne(crt => crt.Computer)
            .WithMany()
            .HasForeignKey(crt => crt.ComputerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(crt => crt.OrganizationId)
            .IsRequired()
            .HasColumnOrder(2);

        builder.HasOne(crt => crt.Organization)
            .WithMany()
            .HasForeignKey(crt => crt.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(crt => crt.PlannedDate)
            .IsRequired()
            .HasColumnOrder(3);

        builder.Property(crt => crt.LastAttemptDate)
            .HasColumnOrder(4);

        builder.Property(crt => crt.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnOrder(5);
    }
}
