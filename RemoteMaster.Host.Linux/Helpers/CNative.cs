// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers;

public static class CNative
{
    private const string LibraryName = "libc";

    [DllImport(LibraryName, SetLastError = true)]
    public static extern uint geteuid();
}
