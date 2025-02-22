// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RemoteMaster.Server.Aggregates.HostMoveRequestAggregate;

namespace RemoteMaster.Server.Configurations;

public class HostMoveRequestConfiguration : IEntityTypeConfiguration<HostMoveRequest>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<HostMoveRequest> builder)
    {
        builder.HasKey(hmr => hmr.Id);

        builder.Property(hmr => hmr.Id)
            .ValueGeneratedOnAdd()
            .HasColumnOrder(0);

        builder.Property(hmr => hmr.MacAddress)
            .IsRequired()
            .HasColumnOrder(1);

        builder.Property(hmr => hmr.Organization)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnOrder(2);

        var listComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (hash, v) => HashCode.Combine(hash, v == null ? 0 : v.GetHashCode())),
            c => c.ToList()
        );

        builder.Property(hmr => hmr.OrganizationalUnit)
            .HasConversion(
                new ValueConverter<List<string>, string>(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)))
            .IsRequired()
            .HasColumnOrder(3)
            .Metadata.SetValueComparer(listComparer);
    }
}
