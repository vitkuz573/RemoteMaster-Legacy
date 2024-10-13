// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.UnitOfWork;

public class TelegramBotUnitOfWork(TelegramBotDbContext context, ITelegramBotRepository telegramBots, ILogger<UnitOfWork<TelegramBotDbContext>> logger) : UnitOfWork<TelegramBotDbContext>(context, logger), ITelegramBotUnitOfWork
{
    public ITelegramBotRepository TelegramBots { get; } = telegramBots;
}
