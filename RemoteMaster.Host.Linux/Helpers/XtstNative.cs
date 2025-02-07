// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers;

public static class XtstNative
{
    private const string LibraryName = "libXtst";

    [DllImport(LibraryName)]
    public static extern bool XTestQueryExtension(nint display, out int event_base, out int error_base, out int major_version, out int minor_version);

    [DllImport(LibraryName)]
    public static extern int XTestFakeKeyEvent(nint display, uint keycode, bool is_press, ulong delay);

    [DllImport(LibraryName)]
    public static extern int XTestFakeButtonEvent(nint display, uint button, bool is_press, ulong delay);

    [DllImport(LibraryName)]
    public static extern int XTestFakeMotionEvent(nint display, int screen_number, int x, int y, ulong delay);
}
