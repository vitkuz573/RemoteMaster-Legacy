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

    // Example constant values (these may differ from the actual values in PipeWire)
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

    public enum SpaParamBuffers : uint
    {
        START = 0,    // Not used directly.
        buffers = 1,  // Number of buffers (Int)
        blocks = 2,   // Number of data blocks per buffer (Int)
        size = 3,     // Size of a data block memory (Int)
        stride = 4,   // Stride of data block memory (Int)
        align = 5,    // Alignment of data block memory (Int)
        dataType = 6, // Possible memory types (flag mask)
        metaType = 7  // Required metadata types (flag mask)
    }

    // For ParamBuffers, we also define the following example values:
    public const uint SPA_TYPE_OBJECT_ParamBuffers = 0x80000001; // Example value
    public const uint SPA_ID_PARAM_BUFFERS = 0;                  // Example value

    // Constants for buffer data types.
    public const int SPA_DATA_DmaBuf = 0;
    public const int SPA_DATA_MemFd = 1;

    /// <summary>
    /// Builds a SPA POD object for buffer parameters.
    /// The layout is as follows:
    /// Header (4 uint32 fields): total_size, type, id, property_count (7)
    /// Then 7 properties, each consisting of a key/value pair.
    /// In this simplified example, the total size is 44 bytes.
    /// </summary>
    /// <param name="buffers">Number of buffers.</param>
    /// <param name="blocks">Number of data blocks per buffer.</param>
    /// <param name="size">Size of a data block (in bytes).</param>
    /// <param name="stride">Stride (in bytes) of a data block.</param>
    /// <param name="align">Memory alignment (in bytes).</param>
    /// <param name="dataType">Data type mask (e.g. 1 << SPA_DATA_MemFd).</param>
    /// <param name="metaType">Metadata type mask (or 0 if none).</param>
    /// <returns>A pointer to the allocated SPA POD buffer parameter blob.</returns>
    public static nint BuildBufferParamPod(uint buffers, uint blocks, uint size, uint stride, uint align, uint dataType, uint metaType)
    {
        // In this example, we use 11 fields: 4 for the header and 7 key/value pairs.
        var totalFields = 11;
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
        Array.Copy(BitConverter.GetBytes((uint)SpaParamBuffers.buffers), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(buffers), 0, blob, offset, 4);
        offset += 4;

        // Property 2: SPA_PARAM_BUFFERS_blocks
        Array.Copy(BitConverter.GetBytes((uint)SpaParamBuffers.blocks), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(blocks), 0, blob, offset, 4);
        offset += 4;

        // Property 3: SPA_PARAM_BUFFERS_size
        Array.Copy(BitConverter.GetBytes((uint)SpaParamBuffers.size), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(size), 0, blob, offset, 4);
        offset += 4;

        // Property 4: SPA_PARAM_BUFFERS_stride
        Array.Copy(BitConverter.GetBytes((uint)SpaParamBuffers.stride), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(stride), 0, blob, offset, 4);
        offset += 4;

        // Property 5: SPA_PARAM_BUFFERS_align
        Array.Copy(BitConverter.GetBytes((uint)SpaParamBuffers.align), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(align), 0, blob, offset, 4);
        offset += 4;

        // Property 6: SPA_PARAM_BUFFERS_dataType
        Array.Copy(BitConverter.GetBytes((uint)SpaParamBuffers.dataType), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(dataType), 0, blob, offset, 4);
        offset += 4;

        // Property 7: SPA_PARAM_BUFFERS_metaType
        Array.Copy(BitConverter.GetBytes((uint)SpaParamBuffers.metaType), 0, blob, offset, 4);
        offset += 4;
        Array.Copy(BitConverter.GetBytes(metaType), 0, blob, offset, 4);
        offset += 4;

        var podPtr = Marshal.AllocHGlobal(totalSize);
        Marshal.Copy(blob, 0, podPtr, totalSize);

        return podPtr;
    }

    #endregion
}
