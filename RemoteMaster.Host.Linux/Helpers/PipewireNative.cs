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
    public static extern nint pw_main_loop_new(nint properties);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_run(nint loop);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_quit(nint loop);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_destroy(nint loop);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_main_loop_get_loop(nint loop);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_new_simple(nint loop, string name, nint properties, ref PwStreamEvents events, nint userData);

    [DllImport(LibPipewire, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_connect(nint stream, int direction, uint targetId, uint flags, nint parameters, uint nParams);

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
        public nint stateChanged;
        public nint process;
        public nint addBuffer;
        public nint removeBuffer;
        public nint drained;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamProcessDelegate(nint userData);

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

    #region SPA POD Builder

    // The video format SPA POD object is built as a binary blob with the following layout:
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

    public static nint BuildVideoFormatPod(uint width, uint height, uint framerateNum, uint framerateDen)
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

    #region SPA PARAM BUFFERS

    // Enumerators for spa_param_buffers.
    public const uint SPA_PARAM_BUFFERS_start = 0; // Not used directly.
    public const uint SPA_PARAM_BUFFERS_buffers = 1;    // Number of buffers (Int)
    public const uint SPA_PARAM_BUFFERS_blocks = 2;     // Number of data blocks per buffer (Int)
    public const uint SPA_PARAM_BUFFERS_size = 3;       // Size of a data block memory (Int)
    public const uint SPA_PARAM_BUFFERS_stride = 4;     // Stride of data block memory (Int)
    public const uint SPA_PARAM_BUFFERS_align = 5;      // Alignment of data block memory (Int)
    public const uint SPA_PARAM_BUFFERS_dataType = 6;   // Possible memory types (flags)
    public const uint SPA_PARAM_BUFFERS_metaType = 7;     // Required meta data types (flags)

    // For our example, we define the object type for ParamBuffers.
    public const uint SPA_TYPE_OBJECT_ParamBuffers = 0x80000001; // Example value
    public const uint SPA_ID_PARAM_BUFFERS = 0;                  // Example value

    // Data type constants for buffer parameters.
    public const int SPA_DATA_DmaBuf = 0;
    public const int SPA_DATA_MemFd = 1;

    /// <summary>
    /// Builds a SPA POD object for buffer parameters.
    /// The layout is as follows:
    /// Header (4 uint32 fields):
    ///   [0] total_size (in bytes), [1] type, [2] id, [3] property_count (7)
    /// Then 7 properties (each 2 uint32 fields): key and value.
    /// Keys are from SPA_PARAM_BUFFERS_* enumerators.
    /// Total fields = 4 + (7 * 2) = 18 fields; however, here we pack each property as one key-value pair.
    /// For our simplified layout we use 11 uint32 fields (4 header + 7 properties) = 44 bytes.
    /// </summary>
    /// <param name="buffers">Number of buffers.</param>
    /// <param name="blocks">Number of data blocks per buffer.</param>
    /// <param name="size">Size of a data block (in bytes).</param>
    /// <param name="stride">Stride (in bytes) of a data block.</param>
    /// <param name="align">Memory alignment in bytes.</param>
    /// <param name="dataType">Data type mask (e.g. 1 << SPA_DATA_MemFd).</param>
    /// <param name="metaType">Meta type mask (or 0 if none).</param>
    /// <returns>An nint pointer to the allocated SPA POD buffer parameter blob.</returns>
    public static nint BuildBufferParamPod(uint buffers, uint blocks, uint size, uint stride, uint align, uint dataType, uint metaType)
    {
        var totalFields = 11; // 4 header + 7 property key/value pairs (each property occupies 1 key and 1 value, but we pack them into 7 fields)
        var totalSize = totalFields * 4; // 44 bytes

        var blob = new byte[totalSize];
        var offset = 0;

        // Header: total_size, type, id, property_count (7)
        Array.Copy(BitConverter.GetBytes((uint)totalSize), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(SPA_TYPE_OBJECT_ParamBuffers), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(SPA_ID_PARAM_BUFFERS), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes((uint)7), 0, blob, offset, 4);
        offset += 4;

        // Property 1: SPA_PARAM_BUFFERS_buffers
        Array.Copy(BitConverter.GetBytes(SPA_PARAM_BUFFERS_buffers), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(buffers), 0, blob, offset, 4);
        offset += 4;

        // Property 2: SPA_PARAM_BUFFERS_blocks
        Array.Copy(BitConverter.GetBytes(SPA_PARAM_BUFFERS_blocks), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(blocks), 0, blob, offset, 4);
        offset += 4;

        // Property 3: SPA_PARAM_BUFFERS_size
        Array.Copy(BitConverter.GetBytes(SPA_PARAM_BUFFERS_size), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(size), 0, blob, offset, 4);
        offset += 4;

        // Property 4: SPA_PARAM_BUFFERS_stride
        Array.Copy(BitConverter.GetBytes(SPA_PARAM_BUFFERS_stride), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(stride), 0, blob, offset, 4);
        offset += 4;

        // Property 5: SPA_PARAM_BUFFERS_align
        Array.Copy(BitConverter.GetBytes(SPA_PARAM_BUFFERS_align), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(align), 0, blob, offset, 4);
        offset += 4;

        // Property 6: SPA_PARAM_BUFFERS_dataType
        Array.Copy(BitConverter.GetBytes(SPA_PARAM_BUFFERS_dataType), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(dataType), 0, blob, offset, 4);
        offset += 4;

        // Property 7: SPA_PARAM_BUFFERS_metaType
        Array.Copy(BitConverter.GetBytes(SPA_PARAM_BUFFERS_metaType), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(metaType), 0, blob, offset, 4);
        offset += 4;

        var podPtr = Marshal.AllocHGlobal(totalSize);
        Marshal.Copy(blob, 0, podPtr, totalSize);

        return podPtr;
    }

    #endregion
}
