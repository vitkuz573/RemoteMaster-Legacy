// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers;

public static class XRandrNative
{
    private const string LibraryName = "libXrandr";

    [StructLayout(LayoutKind.Sequential)]
    public struct XRRMonitorInfo
    {
        // Atom
        public nint name;
        public bool primary;
        public bool automatic;
        public int noutput;
        public int x;
        public int y;
        public int width;
        public int height;
        public int mwidth;
        public int mheight;
        // RROutput*
        public nint outputs;
    }

    [DllImport(LibraryName)]
    public static extern nint XRRGetMonitors(nint display, nint window, bool get_active, out int monitors);

    [DllImport(LibraryName)]
    public static extern void XRRFreeMonitors(nint monitors);

    [DllImport(LibraryName)]
    public static extern nint XRRAllocateMonitor(nint display, int output);
}
