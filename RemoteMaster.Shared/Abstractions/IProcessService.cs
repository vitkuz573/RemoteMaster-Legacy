// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;
using System.Runtime.Versioning;

namespace RemoteMaster.Shared.Abstractions;

[SupportedOSPlatform("windows6.0.6000")]
public interface IProcessService
{
    NativeProcess Start(ProcessStartOptions options);
}