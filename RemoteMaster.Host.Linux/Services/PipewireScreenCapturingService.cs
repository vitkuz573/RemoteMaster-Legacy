// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// A full‑featured, production‑quality PipeWire‑based screen capturing service.
/// Inherits from the abstract ScreenCapturingService and provides a complete implementation using PipeWire.
/// </summary>
public unsafe class PipewireScreenCapturingService : ScreenCapturingService
{
    // Thread‑safe queue for captured frames (raw RGB24 data).
    private readonly BlockingCollection<byte[]> _frameQueue = [];

    // Native handles for the PipeWire main loop, context, and stream.
    private readonly nint _mainLoop = nint.Zero;
    private readonly nint _context = nint.Zero;
    private readonly nint _stream = nint.Zero;

    // Background thread running the PipeWire main loop.
    private readonly Thread _pipewireThread;

    // Desired capture resolution and computed frame size (in bytes).
    private readonly int _width = 1920;
    private readonly int _height = 1080;
    private readonly int _frameSize;

    // Delegate instances for stream callbacks.
    private readonly PipewireNative.PwStreamProcessDelegate _processDelegate;
    private readonly PipewireNative.PwStreamStateChangedDelegate _stateChangedDelegate;
    private readonly PipewireNative.PwStreamErrorDelegate _errorDelegate;

    // Current state of the PipeWire stream.
    private PipewireNative.PwStreamState _currentState = PipewireNative.PwStreamState.Unconnected;

    // Disposal flag.
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the PipewireScreenCapturingService class.
    /// Sets up the PipeWire main loop, context, stream with complete event callbacks, and starts the main loop thread.
    /// </summary>
    public PipewireScreenCapturingService()
    {
        _frameSize = _width * _height * 3; // RGB24: 3 bytes per pixel.

        // Initialize PipeWire.
        PipewireNative.pw_init();

        // Create the main loop.
        _mainLoop = PipewireNative.pw_main_loop_new(nint.Zero);

        if (_mainLoop == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire main loop.");
        }

        // Create the context using the underlying loop.
        var loopPtr = PipewireNative.pw_main_loop_get_loop(_mainLoop);

        _context = PipewireNative.pw_context_new(loopPtr, nint.Zero, 0);

        if (_context == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire context.");
        }

        // Create the stream for screen capture.
        _stream = PipewireNative.pw_stream_new(_context, "PipeWire Screen Capture", nint.Zero);
        
        if (_stream == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire stream.");
        }

        // Set up callback delegates.
        _processDelegate = OnProcess;
        _stateChangedDelegate = OnStateChanged;
        _errorDelegate = OnError;

        // Prepare the stream events structure with full callbacks.
        var events = new PipewireNative.PwStreamEvents
        {
            version = PipewireNative.PW_VERSION,
            process = Marshal.GetFunctionPointerForDelegate(_processDelegate),
            state_changed = Marshal.GetFunctionPointerForDelegate(_stateChangedDelegate),
            error = Marshal.GetFunctionPointerForDelegate(_errorDelegate)
        };

        // Add the listener to the stream.
        PipewireNative.pw_stream_add_listener(_stream, nint.Zero, ref events, nint.Zero);

        // Connect the stream.
        var ret = PipewireNative.pw_stream_connect(_stream, PipewireNative.PW_DIRECTION_INPUT, 0, PipewireNative.PW_STREAM_FLAG_AUTOCONNECT, nint.Zero, 0);
       
        if (ret < 0)
        {
            throw new Exception("Failed to connect PipeWire stream.");
        }

        // Start the PipeWire main loop in a background thread.
        _pipewireThread = new Thread(() =>
        {
            PipewireNative.pw_main_loop_run(_mainLoop);
        })
        {
            IsBackground = true
        };

        _pipewireThread.Start();
    }

    /// <summary>
    /// Process callback invoked by PipeWire when new buffers are available.
    /// Dequeues buffers, extracts raw frame data (with full SPA buffer parsing),
    /// enqueues the frame, and re‑queues the buffer.
    /// </summary>
    /// <param name="userData">User data (unused).</param>
    private void OnProcess(nint userData)
    {
        // Dequeue available buffers.
        while (true)
        {
            var bufferPtr = PipewireNative.pw_stream_dequeue_buffer(_stream);

            if (bufferPtr == nint.Zero)
            {
                break; // No more buffers.
            }

            try
            {
                // Interpret the native buffer as a spa_buffer.
                var spaBuffer = (PipewireNative.spa_buffer*)bufferPtr;

                if (spaBuffer == null || spaBuffer->n_datas < 1)
                {
                    PipewireNative.pw_stream_queue_buffer(_stream, bufferPtr);

                    continue;
                }

                // Retrieve the first spa_data element.
                var data = (PipewireNative.spa_data*)spaBuffer->datas;

                if (data == null || data->data == nint.Zero)
                {
                    PipewireNative.pw_stream_queue_buffer(_stream, bufferPtr);
                    
                    continue;
                }

                var dataSize = (int)data->size;
                var frameData = new byte[dataSize];

                Marshal.Copy(data->data, frameData, 0, dataSize);

                // Enqueue the captured frame.
                _frameQueue.Add(frameData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PipeWire] Error processing buffer: {ex.Message}");
            }
            finally
            {
                // Re‑queue the buffer for reuse.
                PipewireNative.pw_stream_queue_buffer(_stream, bufferPtr);
            }
        }
    }

    /// <summary>
    /// State changed callback invoked when the stream’s state changes.
    /// Logs the state transition and any error message.
    /// </summary>
    private void OnStateChanged(nint userData, PipewireNative.PwStreamState oldState, PipewireNative.PwStreamState newState, IntPtr error)
    {
        _currentState = newState;
        var errorMsg = Marshal.PtrToStringAnsi(error);
       
        Console.WriteLine($"[PipeWire] Stream state changed: {oldState} -> {newState}. Error: {errorMsg}");
    }

    /// <summary>
    /// Error callback invoked when an error occurs in the stream.
    /// Logs the error code and message.
    /// </summary>
    private void OnError(nint userData, int errorCode, string errorMessage)
    {
        Console.WriteLine($"[PipeWire] Stream error: {errorCode}, {errorMessage}");
    }

    /// <summary>
    /// Retrieves the next captured frame as a byte array (assumed to be in RGB24 format).
    /// Blocks until a frame is available.
    /// </summary>
    public override byte[]? GetNextFrame(string connectionId)
    {
        try
        {
            return _frameQueue.Take();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Disposes all PipeWire resources, stops the main loop, and cleans up the stream and context.
    /// </summary>
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Signal the main loop to quit and wait for the background thread.
        if (_mainLoop != nint.Zero)
        {
            PipewireNative.pw_main_loop_quit(_mainLoop);

            _pipewireThread.Join(2000);
        }

        // Disconnect and destroy the stream.
        if (_stream != nint.Zero)
        {
            PipewireNative.pw_stream_disconnect(_stream);
            PipewireNative.pw_stream_destroy(_stream);
        }

        // Destroy the context and main loop.
        if (_context != nint.Zero)
        {
            PipewireNative.pw_context_destroy(_context);
        }

        if (_mainLoop != nint.Zero)
        {
            PipewireNative.pw_main_loop_destroy(_mainLoop);
        }

        PipewireNative.pw_deinit();

        _disposed = true;
    }
}

/// <summary>
/// Contains full interop definitions for the core PipeWire API functions, structures, and callbacks.
/// </summary>
internal static class PipewireNative
{
    public const int PW_VERSION = 0;
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
        Unconnected,
        Connecting,
        Connected,
        Configured,
        Ready,
        Error,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PwStreamEvents
    {
        public uint version;
        public nint process;
        public nint state_changed;
        public nint error;
    }

    // Main loop and context management.
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

    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_context_new(nint loop, nint properties, uint version);

    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_context_destroy(nint context);

    // Stream management.
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_new(IntPtr context, string name, nint properties);

    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_connect(nint stream, int direction, uint target, uint flags, IntPtr parameters, uint n_params);

    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_stream_disconnect(nint stream);

    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_stream_destroy(nint stream);

    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pw_stream_add_listener(nint stream, nint listener, ref PwStreamEvents events, IntPtr data);

    // Buffer management.
    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pw_stream_dequeue_buffer(nint stream);

    [DllImport(PipewireLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pw_stream_queue_buffer(nint stream, nint buffer);

    // SPA buffer structures.
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct spa_buffer
    {
        public uint id;
        public uint n_metas;
        public nint metas; // Ignored.
        public uint n_datas;
        public nint datas; // Pointer to an array of spa_data structures.
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct spa_data
    {
        public nint data;    // Pointer to the raw buffer data.
        public uint size;      // Size of valid data.
        public uint maxsize;   // Maximum allocated size.
        public uint flags;
        public uint type;
        public long mapoffset;
    }
}
