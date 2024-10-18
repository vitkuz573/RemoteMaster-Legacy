// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.CertificateRenewalTaskAggregate;
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

        builder.Property(crt => crt.HostId)
            .IsRequired()
            .HasColumnOrder(1);

        builder.Property(crt => crt.PlannedDate)
            .IsRequired()
            .HasColumnOrder(4);

        builder.Property(crt => crt.LastAttemptDate)
            .HasColumnOrder(5);

        builder.Property(crt => crt.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnOrder(6);
    }
}
