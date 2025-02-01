// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Helpers;

/// <summary>
/// Contains interop definitions for the core PipeWire API functions, structures, and callbacks.
/// This mapping is based on PipeWire 1.2.7’s stream.h.
/// </summary>
internal static class PipewireNative
{
    // Macro for stream events version.
    public const int PW_VERSION_STREAM_EVENTS = 2;
    public const int PW_DIRECTION_INPUT = 0;
    public const int PW_STREAM_FLAG_AUTOCONNECT = 1;

    private const string PipewireLib = "libpipewire-0.3.so";

    // Delegate definitions.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamProcessDelegate(nint userData);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamStateChangedDelegate(nint userData, PwStreamState oldState, PwStreamState newState, nint error);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PwStreamErrorDelegate(nint userData, int error, [MarshalAs(UnmanagedType.LPStr)] string errorMessage);

    public enum PwStreamState
    {
        Error = -1,
        Unconnected = 0,
        Connecting = 1,
        Paused = 2,
        Streaming = 3,
    }

    // Full stream events structure.
    [StructLayout(LayoutKind.Sequential)]
    public struct PwStreamEvents
    {
        public uint version;
        public nint process;
        public nint state_changed;
        public nint error;
        // Optional additional callbacks:
        public nint destroy;
        public nint control_info;
        public nint io_changed;
        public nint param_changed;
        public nint add_buffer;
        public nint remove_buffer;
        public nint drained;
        public nint command;
        public nint trigger_done;
    }

    // Main loop and context functions.
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_init();
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_deinit();
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_main_loop_new(nint properties);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_run(nint loop);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_quit(nint loop);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_main_loop_destroy(nint loop);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_main_loop_get_loop(nint loop);

    // Simplified stream creation using pw_stream_new_simple.
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_new_simple(nint loop, string name, nint props, ref PwStreamEvents events, nint data);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_connect(nint stream, int direction, uint target_id, uint flags, nint parameters, uint n_params);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_set_active(nint stream, bool active);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_stream_disconnect(nint stream);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_stream_destroy(nint stream);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_dequeue_buffer(nint stream);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_queue_buffer(nint stream, nint buffer);
    
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_state_as_string(PwStreamState state);

    // Additional structures from pipewire/stream.h:

    // spa_fraction structure.
    [StructLayout(LayoutKind.Sequential)]
    public struct spa_fraction
    {
        public int num;
        public int denom;
    }

    // spa_buffer structure.
    [StructLayout(LayoutKind.Sequential)]
    public struct spa_buffer
    {
        public uint id;
        public uint n_metas;
        public nint metas;   // Typically ignored.
        public uint n_datas;
        public nint datas;   // Pointer to an array of spa_data structures.
    }

    // spa_data structure.
    [StructLayout(LayoutKind.Sequential)]
    public struct spa_data
    {
        public nint data;    // Pointer to the raw buffer data.
        public uint size;      // Size of valid data.
        public uint maxsize;   // Maximum allocated size.
        public uint flags;
        public uint type;
        public long mapoffset;
    }

    // pw_buffer structure.
    [StructLayout(LayoutKind.Sequential)]
    public struct pw_buffer
    {
        public nint buffer;      // Pointer to a spa_buffer.
        public nint user_data;   // User data attached to the buffer.
        public ulong size;       // Sum of all queued buffer sizes.
        public ulong requested;  // For playback streams.
        public ulong time;       // For capture streams.
    }

    // pw_stream_control structure.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct pw_stream_control
    {
        public nint name;        // const char* (convert via Marshal.PtrToStringAnsi)
        public uint flags;
        public float def;
        public float min;
        public float max;
        public nint values;      // Pointer to a float array.
        public uint n_values;
        public uint max_values;
    }

    // pw_time structure.
    [StructLayout(LayoutKind.Sequential)]
    public struct pw_time
    {
        public long now;         // Time in nanoseconds.
        public spa_fraction rate;// Rate of ticks and delay.
        public ulong ticks;      // Monotonic ticks.
        public long delay;       // Delay to device.
        public ulong queued;     // Sum of sizes in queued buffers.
        public ulong buffered;   // Extra frames buffered.
        public uint queued_buffers;
        public uint avail_buffers;
        public ulong size;       // Number of samples (playback/capture).
    }
}
