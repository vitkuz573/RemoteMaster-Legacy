// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Data;

public class TokenDbContext(DbContextOptions<TokenDbContext> options) : DbContext(options)
{
    public DbSet<TokenEntity> Tokens { get; set; }
}