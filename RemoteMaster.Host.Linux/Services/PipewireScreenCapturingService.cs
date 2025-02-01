// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using RemoteMaster.Host.Linux.Helpers;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// A full‑quality PipeWire–based screen capturing service.
/// Inherits from ScreenCapturingService and uses SPA POD–based format negotiation and real
/// parsing of PipeWire buffers.
/// </summary>
public unsafe class PipewireScreenCapturingService : ScreenCapturingService
{
    private readonly nint _mainLoop;
    private readonly nint _stream;
    private readonly Thread _pipewireThread;
    private readonly BlockingCollection<byte[]> _frameQueue = [];

    private readonly int _width;
    private readonly int _height;
    private readonly int _bytesPerPixel;
    private readonly int _frameSize;
    private readonly uint _framerateNum;
    private readonly uint _framerateDen;

    // Unmanaged pointer to the SPA POD video format description.
    private readonly nint _formatPod;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of PipewireScreenCapturingService.
    /// </summary>
    /// <param name="width">Desired video width in pixels.</param>
    /// <param name="height">Desired video height in pixels.</param>
    /// <param name="bytesPerPixel">Bytes per pixel (e.g. 3 for RGB24).</param>
    /// <param name="framerateNum">Framerate numerator (e.g. 30).</param>
    /// <param name="framerateDen">Framerate denominator (e.g. 1).</param>
    public PipewireScreenCapturingService(int width, int height, int bytesPerPixel = 3, uint framerateNum = 30, uint framerateDen = 1)
    {
        _width = width;
        _height = height;
        _bytesPerPixel = bytesPerPixel;
        _frameSize = _width * _height * _bytesPerPixel;
        _framerateNum = framerateNum;
        _framerateDen = framerateDen;

        // Initialize PipeWire.
        PipewireNative.pw_init();

        _mainLoop = PipewireNative.pw_main_loop_new(nint.Zero);

        if (_mainLoop == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire main loop.");
        }

        var loopPtr = PipewireNative.pw_main_loop_get_loop(_mainLoop);

        // Set up the stream events, including our process callback.
        var events = new PipewireNative.PwStreamEvents
        {
            version = 1,
            stateChanged = nint.Zero,
            process = Marshal.GetFunctionPointerForDelegate(new PipewireNative.PwStreamProcessDelegate(ProcessCallback)),
            addBuffer = nint.Zero,
            removeBuffer = nint.Zero,
            drained = nint.Zero
        };

        _stream = PipewireNative.pw_stream_new_simple(loopPtr, "PipeWire Screen Capture", nint.Zero, ref events, nint.Zero);
        
        if (_stream == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire stream.");
        }

        // Build the full SPA POD for video format negotiation.
        _formatPod = PipewireNative.BuildVideoFormatPod((uint)_width, (uint)_height, _framerateNum, _framerateDen);

        var ret = PipewireNative.pw_stream_connect(_stream, PipewireNative.PW_DIRECTION_INPUT, 0, PipewireNative.PW_STREAM_FLAG_AUTOCONNECT, _formatPod, 1);
        
        if (ret < 0)
        {
            throw new Exception("Failed to connect PipeWire stream.");
        }

        ret = PipewireNative.pw_stream_set_active(_stream, true);
        
        if (ret < 0)
        {
            throw new Exception("Failed to activate PipeWire stream.");
        }

        // Start the PipeWire main loop on a background thread.
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
    /// Dequeues a buffer, parses the spa_buffer structure to extract raw frame data,
    /// enqueues the frame, and re-queues the buffer.
    /// </summary>
    /// <param name="userData">User data pointer (unused).</param>
    private void ProcessCallback(nint userData)
    {
        while (true)
        {
            var bufferPtr = PipewireNative.pw_stream_dequeue_buffer(_stream);

            if (bufferPtr == nint.Zero)
            {
                break;
            }

            try
            {
                var spaBuf = (PipewireNative.spa_buffer*)bufferPtr;
                
                if (spaBuf == null || spaBuf->n_datas < 1)
                {
                    continue;
                }

                var spaData = (PipewireNative.spa_data*)spaBuf->datas;
                
                if (spaData == null || spaData->data == nint.Zero)
                {
                    continue;
                }

                var dataSize = (int)spaData->size;
                var frameData = new byte[dataSize];

                Marshal.Copy(spaData->data, frameData, 0, dataSize);

                _frameQueue.Add(frameData);
            }
            finally
            {
                PipewireNative.pw_stream_queue_buffer(_stream, bufferPtr);
            }
        }
    }

    /// <summary>
    /// Retrieves the next captured frame as a byte array.
    /// The connectionId parameter is ignored in this implementation.
    /// </summary>
    /// <param name="connectionId">A connection identifier.</param>
    /// <returns>Raw frame data or null if unavailable.</returns>
    public override byte[]? GetNextFrame(string connectionId)
    {
        return _frameQueue.Take();
    }

    /// <summary>
    /// Disposes all PipeWire resources, stops the main loop, destroys the stream,
    /// frees the SPA POD memory, and deinitializes PipeWire.
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
        if (_formatPod != nint.Zero)
        {
            Marshal.FreeHGlobal(_formatPod);
        }

        PipewireNative.pw_deinit();

        _disposed = true;
    }
}
