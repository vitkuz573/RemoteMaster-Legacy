// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RemoteMaster.Server.Aggregates.TelegramBotAggregate;

namespace RemoteMaster.Server.Configurations;

public class TelegramBotChatIdConfiguration : IEntityTypeConfiguration<TelegramBotChatId>
{
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "EntityTypeBuilder will not be null.")]
    public void Configure(EntityTypeBuilder<TelegramBotChatId> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ChatId)
            .IsRequired()
            .HasMaxLength(100);

        builder.ToTable("TelegramBotChatIds");
    }
}
