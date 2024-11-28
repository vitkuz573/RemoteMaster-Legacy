// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Tests;

public class TestProcessStartInfo(string fileName = "test.exe", string arguments = "") : INativeProcessStartInfo
{
    public ProcessStartInfo ProcessStartInfo { get; } = new ProcessStartInfo(fileName, arguments);
}
