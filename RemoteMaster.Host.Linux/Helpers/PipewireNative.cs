// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers;

internal static class PipewireNative
{
    private const string LibPipewire = "libpipewire-0.3.so";

    public const int PW_DIRECTION_INPUT = 0;
    public const int PW_STREAM_FLAG_AUTOCONNECT = 1;

    #region PipeWire API P/Invoke

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_init();

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_deinit();

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_main_loop_new(IntPtr properties);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_run(nint loop);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_quit(nint loop);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_destroy(nint loop);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_main_loop_get_loop(nint loop);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_new_simple(nint loop, string name, IntPtr properties, ref PwStreamEvents events, IntPtr userData);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_connect(nint stream, int direction, uint targetId, uint flags, IntPtr parameters, uint nParams);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_set_active(nint stream, bool active);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_stream_disconnect(nint stream);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_stream_destroy(nint stream);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_dequeue_buffer(nint stream);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_queue_buffer(nint stream, nint buffer);

    #endregion

    #region Stream Events and Delegates

    [StructLayout(LayoutKind.Sequential)]
    public struct PwStreamEvents
    {
        public uint version;
        public IntPtr stateChanged;
        public IntPtr process;
        public IntPtr addBuffer;
        public IntPtr removeBuffer;
        public IntPtr drained;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamProcessDelegate(IntPtr userData);

    #endregion

    #region SPA Buffer Structures

    [StructLayout(LayoutKind.Sequential)]
    public struct spa_buffer
    {
        public uint id;
        public uint n_metas;
        public nint metas;
        public uint n_datas;
        public nint datas; // Pointer to an array of spa_data structures
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct spa_data
    {
        public nint data;    // Pointer to raw data
        public uint size;    // Valid data size
        public uint maxsize; // Allocated size
        public uint flags;
        public uint type;
        public long mapoffset;
    }

    #endregion

    #region SPA POD Builder (Full Implementation)

    // The SPA POD object is built as a binary blob with the following layout:
    // Header (4 uint32 fields):
    //   [0] total_size:    total size in bytes (40)
    //   [1] type:          object type (SPA_TYPE_OBJECT_Format)
    //   [2] id:            object id (SPA_ID_FORMAT)
    //   [3] property_count: number of properties (2)
    // Then two properties (each 3 uint32 fields):
    //   Property 1: [4] key (SPA_FORMAT_VIDEO_size), [5] width, [6] height
    //   Property 2: [7] key (SPA_FORMAT_VIDEO_framerate), [8] numerator, [9] denominator
    // Total fields = 10, total size = 40 bytes.
    public const uint SPA_TYPE_OBJECT_Format = 0x80000000; // Example value
    public const uint SPA_ID_FORMAT = 0;                    // Example value
    public const uint SPA_FORMAT_VIDEO_size = 1;            // Key for video size property
    public const uint SPA_FORMAT_VIDEO_framerate = 2;       // Key for video framerate property

    public static IntPtr BuildVideoFormatPod(uint width, uint height, uint framerateNum, uint framerateDen)
    {
        var totalFields = 10;
        var totalSize = totalFields * 4; // 40 bytes

        var buffer = new byte[totalSize];
        var offset = 0;

        Array.Copy(BitConverter.GetBytes((uint)totalSize), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(SPA_TYPE_OBJECT_Format), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(SPA_ID_FORMAT), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes((uint)2), 0, buffer, offset, 4);
        offset += 4;

        Array.Copy(BitConverter.GetBytes(SPA_FORMAT_VIDEO_size), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(width), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(height), 0, buffer, offset, 4);
        offset += 4;

        Array.Copy(BitConverter.GetBytes(SPA_FORMAT_VIDEO_framerate), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(framerateNum), 0, buffer, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(framerateDen), 0, buffer, offset, 4);
        offset += 4;

        var podPtr = Marshal.AllocHGlobal(totalSize);
        Marshal.Copy(buffer, 0, podPtr, totalSize);

        return podPtr;
    }

    #endregion
}
