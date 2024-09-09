// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Entities;

public class ConfigurationSyncState
{
    public int Id { get; set; }
    
    public bool IsSyncRequired { get; set; }
    
    public DateTimeOffset LastAttempt { get; set; }
}
