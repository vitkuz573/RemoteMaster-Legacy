// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Windows.Win32.Foundation;

internal partial class Interop
{
    internal static class PARAM
    {
        public static nint FromLowHigh(int low, int high) => ToInt(low, high);

        public static nint FromLowHighUnsigned(int low, int high) => (nint)(uint)ToInt(low, high);

        public static int ToInt(int low, int high) => (high << 16) | (low & 0xffff);

        public static int HIWORD(int n) => (n >> 16) & 0xffff;

        public static int LOWORD(int n) => n & 0xffff;

        public static int LOWORD(nint n) => LOWORD((int)n);

        public static int HIWORD(nint n) => HIWORD((int)n);

        public static int SignedHIWORD(nint n) => SignedHIWORD((int)n);

        public static int SignedLOWORD(nint n) => SignedLOWORD(unchecked((int)n));

        public static int SignedHIWORD(int n) => (short)HIWORD(n);

        public static int SignedLOWORD(int n) => (short)LOWORD(n);

        public static nint FromBool(bool value) => (nint)(BOOL)(value);

        public static int ToInt(nint param) => (int)param;

        public static uint ToUInt(nint param) => (uint)param;

        public static nint FromPoint(Point point) => FromLowHigh(point.X, point.Y);

        public static Point ToPoint(nint param) => new(SignedLOWORD(param), SignedHIWORD(param));
    }
}