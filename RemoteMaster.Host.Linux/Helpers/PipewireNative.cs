// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers;
public static class PipewireNative
{
    private const string LIB = "libpipewire-0.3";

    // Initialize PipeWire.
    [DllImport(LIB)]
    public static extern void pw_init();

    // Create a new main loop.
    [DllImport(LIB)]
    public static extern nint pw_main_loop_new(nint properties);

    // Get the underlying loop pointer.
    [DllImport(LIB)]
    public static extern nint pw_main_loop_get_loop(nint mainLoop);

    // Run the main loop (this call blocks).
    [DllImport(LIB)]
    public static extern void pw_main_loop_run(nint mainLoop);

    // Quit the main loop.
    [DllImport(LIB)]
    public static extern void pw_main_loop_quit(nint mainLoop);

    // Destroy the main loop.
    [DllImport(LIB)]
    public static extern void pw_main_loop_destroy(nint mainLoop);

    // Create a new stream in "simple" mode.
    [DllImport(LIB)]
    public static extern nint pw_stream_new_simple(nint loop,
                                                   [MarshalAs(UnmanagedType.LPStr)] string name,
                                                   nint properties,
                                                   ref PwStreamEvents events,
                                                   nint userData);

    // Connect the stream.
    [DllImport(LIB)]
    public static extern int pw_stream_connect(nint stream,
                                               int direction,
                                               uint id,
                                               uint flags,
                                               nint parameters,
                                               int n_parameters);

    // Dequeue a buffer.
    [DllImport(LIB)]
    public static extern nint pw_stream_dequeue_buffer(nint stream);

    // Queue a buffer.
    [DllImport(LIB)]
    public static extern void pw_stream_queue_buffer(nint stream, nint buffer);

    // Disconnect the stream.
    [DllImport(LIB)]
    public static extern void pw_stream_disconnect(nint stream);

    // Destroy the stream.
    [DllImport(LIB)]
    public static extern void pw_stream_destroy(nint stream);

    // --- Definitions for properties functions ---
    // A simplified overload for pw_properties_new that supports two key/value pairs.
    [DllImport(LIB, EntryPoint = "pw_properties_new")]
    public static extern nint pw_properties_new(
        [MarshalAs(UnmanagedType.LPStr)] string key1,
        [MarshalAs(UnmanagedType.LPStr)] string value1,
        [MarshalAs(UnmanagedType.LPStr)] string key2,
        [MarshalAs(UnmanagedType.LPStr)] string value2,
        nint end); // Pass IntPtr.Zero as sentinel.

    // Set a property on an existing properties object.
    [DllImport(LIB, EntryPoint = "pw_properties_set")]
    public static extern void pw_properties_set(nint properties,
                                                [MarshalAs(UnmanagedType.LPStr)] string key,
                                                [MarshalAs(UnmanagedType.LPStr)] string value);

    // --- Helpers for building SPA PODs ---
    // These functions are placeholders. In a real implementation you must build valid SPA PODs.
    public static nint BuildVideoFormatPod(uint width, uint height, uint framerateNum, uint framerateDen)
    {
        int size = 128;
        nint pod = Marshal.AllocHGlobal(size);
        // Initialize memory with valid POD data.
        return pod;
    }

    public static nint BuildBufferParamPod(uint buffers, uint blocks, uint size, uint stride, uint align, int dataType, int metaType)
    {
        int allocSize = 128;
        nint pod = Marshal.AllocHGlobal(allocSize);
        // Fill in the structure with buffer parameter data.
        return pod;
    }

    // --- Helper to extract frame data from a PipeWire buffer ---
    // This simplified version assumes that the buffer pointer points directly to the frame data.
    public static byte[] ExtractFrameData(nint bufferPtr, int expectedSize)
    {
        byte[] data = new byte[expectedSize];
        Marshal.Copy(bufferPtr, data, 0, expectedSize);
        return data;
    }

    // --- Constants used in PipeWire calls ---
    public const int PW_DIRECTION_INPUT = 0;
    public const uint PW_ID_ANY = 0;
    public const uint PW_STREAM_FLAG_AUTOCONNECT = 1;

    // --- Delegate and structure definitions for stream events ---
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamProcessDelegate(nint userData);

    [StructLayout(LayoutKind.Sequential)]
    public struct PwStreamEvents
    {
        public uint version;
        public nint stateChanged; // Callback for state changes (optional)
        public nint process;      // Process callback (required)
        public nint addBuffer;    // Optional
        public nint removeBuffer; // Optional
        public nint drained;      // Optional
    }
}
