// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Entities;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Models;

public class HostInfo(string name, string ipAddress, string macAddress) : Computer(name, ipAddress, macAddress);
