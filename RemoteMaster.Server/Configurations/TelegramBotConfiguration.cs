// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.TelegramBotAggregate;

namespace RemoteMaster.Server.Configurations;

public class TelegramBotConfiguration : IEntityTypeConfiguration<TelegramBot>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<TelegramBot> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.IsEnabled)
            .IsRequired();

        builder.Property(t => t.BotToken)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(t => t.ChatIds)
            .WithOne(c => c.TelegramBot)
            .HasForeignKey(c => c.TelegramBotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
