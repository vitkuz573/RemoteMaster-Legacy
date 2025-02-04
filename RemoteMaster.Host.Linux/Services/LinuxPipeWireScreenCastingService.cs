// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Linux.Helpers;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// A Linux-specific screen casting service implementation using a persistent PipeWire stream.
/// </summary>
public class LinuxPipeWireScreenCastingService : IScreenCastingService, IDisposable
{
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private readonly ILogger<LinuxPipeWireScreenCastingService> _logger;

    // PipeWire native pointers.
    private nint _mainLoop;
    private nint _stream;
    private Thread? _pipeWireThread;

    // A concurrent queue for frames.
    private readonly ConcurrentQueue<byte[]> _frameQueue = new ConcurrentQueue<byte[]>();

    // Video parameters.
    private readonly int _width = 1920;
    private readonly int _height = 1080;
    private readonly int _bytesPerPixel = 3; // For RGB24.
    private readonly int _frameSize; // Calculated as width * height * bytesPerPixel.

    public LinuxPipeWireScreenCastingService(
        IHubContext<ControlHub, IControlClient> hubContext,
        ILogger<LinuxPipeWireScreenCastingService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _frameSize = _width * _height * _bytesPerPixel;
        InitializePipeWire();
    }

    private void InitializePipeWire()
    {
        // Initialize PipeWire.
        PipewireNative.pw_init();

        // Create a main loop.
        _mainLoop = PipewireNative.pw_main_loop_new(nint.Zero);
        if (_mainLoop == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire main loop.");
        }

        nint loop = PipewireNative.pw_main_loop_get_loop(_mainLoop);

        // Create properties for the stream.
        nint props = PipewireNative.pw_properties_new(
            "media.class", "Video/Source",
            "media.role", "Screen",
            IntPtr.Zero);
        if (props == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire properties.");
        }

        PipewireNative.pw_properties_set(props, "media.role", "Screen");

        // Define stream events with a process callback.
        var events = new PipewireNative.PwStreamEvents
        {
            version = 1,
            process = Marshal.GetFunctionPointerForDelegate(new PipewireNative.PwStreamProcessDelegate(ProcessCallback))
        };

        // Create the stream.
        _stream = PipewireNative.pw_stream_new_simple(loop, "PipeWire Screen Capture", props, ref events, nint.Zero);
        if (_stream == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire stream.");
        }

        // Build negotiation parameters (SPA PODs).
        nint formatPod = PipewireNative.BuildVideoFormatPod((uint)_width, (uint)_height, 30, 1);
        nint bufferPod = PipewireNative.BuildBufferParamPod(4, 1, (uint)_frameSize, (uint)(_width * _bytesPerPixel), 4096, 1 << 0, 0);

        // Create an array of parameters.
        nint[] parameters = { formatPod, bufferPod };
        nint paramArray = Marshal.AllocHGlobal(2 * nint.Size);
        Marshal.Copy(parameters, 0, paramArray, 2);

        int ret = PipewireNative.pw_stream_connect(_stream,
            PipewireNative.PW_DIRECTION_INPUT,
            PipewireNative.PW_ID_ANY,
            PipewireNative.PW_STREAM_FLAG_AUTOCONNECT,
            paramArray,
            2);
        if (ret < 0)
        {
            throw new Exception("Failed to connect PipeWire stream.");
        }

        // Start the main loop on a background thread.
        _pipeWireThread = new Thread(() =>
        {
            PipewireNative.pw_main_loop_run(_mainLoop);
        })
        {
            IsBackground = true
        };
        _pipeWireThread.Start();
    }

    // This process callback is invoked when a new buffer is available.
    private void ProcessCallback(nint userData)
    {
        nint bufferPtr = PipewireNative.pw_stream_dequeue_buffer(_stream);
        if (bufferPtr == nint.Zero)
        {
            return;
        }

        try
        {
            // Extract the frame data from the buffer.
            byte[] frameData = PipewireNative.ExtractFrameData(bufferPtr, _frameSize);
            _frameQueue.Enqueue(frameData);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in PipeWire process callback: {Message}", ex.Message);
        }
        finally
        {
            PipewireNative.pw_stream_queue_buffer(_stream, bufferPtr);
        }
    }

    /// <summary>
    /// Starts streaming screen data to the specified viewer.
    /// </summary>
    public void StartStreaming(IViewer viewer)
    {
        if (viewer == null)
        {
            throw new ArgumentNullException(nameof(viewer));
        }

        _logger.LogInformation("Starting Linux PipeWire screen streaming for connection ID {ConnectionId}, User: {UserName}",
            viewer.ConnectionId, viewer.UserName);

        // Start a background task to send frames continuously.
        Task.Run(async () =>
        {
            while (!viewer.CancellationTokenSource.Token.IsCancellationRequested)
            {
                int delay = 1000 / viewer.CapturingContext.FrameRate;
                if (_frameQueue.TryDequeue(out byte[] frame))
                {
                    await _hubContext.Clients.Client(viewer.ConnectionId).ReceiveScreenUpdate(frame);
                }
                await Task.Delay(delay, viewer.CancellationTokenSource.Token);
            }
        }, viewer.CancellationTokenSource.Token);
    }

    public void Dispose()
    {
        if (_mainLoop != nint.Zero)
        {
            PipewireNative.pw_main_loop_quit(_mainLoop);
        }
        _pipeWireThread?.Join(2000);
        if (_stream != nint.Zero)
        {
            PipewireNative.pw_stream_disconnect(_stream);
            PipewireNative.pw_stream_destroy(_stream);
        }
        if (_mainLoop != nint.Zero)
        {
            PipewireNative.pw_main_loop_destroy(_mainLoop);
        }
        // Free any additional allocated memory (SPA PODs, parameter array, etc.) as needed.
    }
}
