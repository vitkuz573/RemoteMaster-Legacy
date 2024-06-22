// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;
public class ViewerDto
{
    public string ConnectionId { get; set; }

    public string Group { get; set; }

    public string UserName { get; set; }

    public string Role { get; set; }

    public DateTime ConnectedTime { get; set; }
}
