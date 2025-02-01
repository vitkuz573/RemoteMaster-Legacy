// Copyright © 2023 Vitaly Kuzyaev.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using RemoteMaster.Host.Linux.Helpers;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// A full‑featured, production‑quality PipeWire‑based screen capturing service.
/// Inherits from the abstract ScreenCapturingService and provides a complete implementation using PipeWire.
/// </summary>
public unsafe class PipewireScreenCapturingService : ScreenCapturingService
{
    // Thread‑safe queue for captured frames (raw RGB24 data).
    private readonly BlockingCollection<byte[]> _frameQueue = [];

    // Native handle for the PipeWire main loop and stream.
    private readonly nint _mainLoop = nint.Zero;
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
    /// Sets up the PipeWire main loop, creates the stream using pw_stream_new_simple (which registers event callbacks in one call),
    /// connects and activates the stream, and starts the main loop in a background thread.
    /// </summary>
    public PipewireScreenCapturingService()
    {
        _frameSize = _width * _height * 3; // RGB24: 3 bytes per pixel

        // Initialize PipeWire.
        PipewireNative.pw_init();

        // Create the PipeWire main loop.
        _mainLoop = PipewireNative.pw_main_loop_new(nint.Zero);

        if (_mainLoop == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire main loop.");
        }

        // Prepare the stream events structure.
        _processDelegate = OnProcess;
        _stateChangedDelegate = OnStateChanged;
        _errorDelegate = OnError;

        var events = new PipewireNative.PwStreamEvents
        {
            version = PipewireNative.PW_VERSION_STREAM_EVENTS, // Use the version macro from docs.
            process = Marshal.GetFunctionPointerForDelegate(_processDelegate),
            state_changed = Marshal.GetFunctionPointerForDelegate(_stateChangedDelegate),
            error = Marshal.GetFunctionPointerForDelegate(_errorDelegate)
        };

        // Retrieve the underlying loop pointer.
        var loopPtr = PipewireNative.pw_main_loop_get_loop(_mainLoop);

        // Create a new stream using the simple API, which registers the events.
        _stream = PipewireNative.pw_stream_new_simple(loopPtr, "PipeWire Screen Capture", nint.Zero, ref events, nint.Zero);
       
        if (_stream == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire stream.");
        }

        // Connect the stream.
        var ret = PipewireNative.pw_stream_connect(_stream, PipewireNative.PW_DIRECTION_INPUT, 0, PipewireNative.PW_STREAM_FLAG_AUTOCONNECT, nint.Zero, 0);
        
        if (ret < 0)
        {
            throw new Exception("Failed to connect PipeWire stream.");
        }

        // Activate the stream explicitly.
        ret = PipewireNative.pw_stream_set_active(_stream, true);

        if (ret < 0)
        {
            throw new Exception("Failed to activate PipeWire stream.");
        }

        // Start the main loop in a background thread.
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
    /// Dequeues buffers, extracts raw frame data (using SPA buffer parsing), enqueues the frame,
    /// and re‑queues the buffer for reuse.
    /// </summary>
    /// <param name="userData">User data (unused).</param>
    private void OnProcess(nint userData)
    {
        // Dequeue available buffers in a loop.
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
                var spaData = (PipewireNative.spa_data*)spaBuffer->datas;
                
                if (spaData == null || spaData->data == nint.Zero)
                {
                    PipewireNative.pw_stream_queue_buffer(_stream, bufferPtr);
                    
                    continue;
                }

                var dataSize = (int)spaData->size;
                var frameData = new byte[dataSize];
                
                Marshal.Copy(spaData->data, frameData, 0, dataSize);

                // Enqueue the captured frame.
                _frameQueue.Add(frameData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PipeWire] Error processing buffer: {ex.Message}");
            }
            finally
            {
                // Re‑queue the buffer so it can be reused.
                PipewireNative.pw_stream_queue_buffer(_stream, bufferPtr);
            }
        }
    }

    /// <summary>
    /// State changed callback invoked when the stream’s state changes.
    /// Logs the state transition and any error message.
    /// </summary>
    private void OnStateChanged(nint userData, PipewireNative.PwStreamState oldState, PipewireNative.PwStreamState newState, nint error)
    {
        _currentState = newState;
        
        var errorMsg = Marshal.PtrToStringAnsi(error);
        var oldStateStr = Marshal.PtrToStringAnsi(PipewireNative.pw_stream_state_as_string(oldState));
        var newStateStr = Marshal.PtrToStringAnsi(PipewireNative.pw_stream_state_as_string(newState));
        
        Console.WriteLine($"[PipeWire] Stream state changed: {oldStateStr} -> {newStateStr}. Error: {errorMsg}");
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
    /// Disposes all PipeWire resources, stops the main loop, and cleans up the stream.
    /// </summary>
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_mainLoop != nint.Zero)
        {
            PipewireNative.pw_main_loop_quit(_mainLoop);

            _pipewireThread.Join(2000);
        }

        if (_stream != nint.Zero)
        {
            PipewireNative.pw_stream_disconnect(_stream);
            PipewireNative.pw_stream_destroy(_stream);
        }

        if (_mainLoop != nint.Zero)
        {
            PipewireNative.pw_main_loop_destroy(_mainLoop);
        }

        PipewireNative.pw_deinit();

        _disposed = true;
    }
}
