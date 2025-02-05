// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Linux.Helpers.ScreenHelper;
using static RemoteMaster.Host.Linux.Helpers.PipewireNative;

namespace RemoteMaster.Host.Linux.Services;

public class ScreenCastingService : IScreenCastingService, IDisposable
{
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private readonly ILogger<ScreenCastingService> _logger;

    private nint _mainLoop;
    private nint _stream;
    private Thread? _pipeWireThread;

    private readonly ConcurrentQueue<byte[]> _frameQueue = new();

    private int _width;
    private int _height;
    private readonly int _bytesPerPixel = 3; // For RGB24.
    private int _frameSize; // width * height * bytesPerPixel

    // Delegates for stream events.
    private PwStreamProcessDelegate _processDelegate;
    private PwStreamStateChangedDelegate _stateChangedDelegate;

    public ScreenCastingService(IHubContext<ControlHub, IControlClient> hubContext, ILogger<ScreenCastingService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _frameSize = _width * _height * _bytesPerPixel;

        InitializePipeWire();
    }

    private void InitializePipeWire()
    {
        var primaryScreen = new Screen(Rectangle.Empty, "dummy", false);

        // var primaryScreen = Screen.PrimaryScreen;

        if (primaryScreen != null)
        {
            _width = primaryScreen.Bounds.Width;
            _height = primaryScreen.Bounds.Height;
        }
        else
        {
            // Fallback values if no screen was found
            _width = 1920;
            _height = 1080;
        }

        _frameSize = _width * _height * _bytesPerPixel;

        var argc = 0;
        var argv = nint.Zero;

        pw_init(ref argc, ref argv);

        _mainLoop = pw_main_loop_new(nint.Zero);
        if (_mainLoop == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire main loop.");
        }

        // Create stream properties.
        var props = pw_properties_new(PW_KEY_MEDIA_CLASS, "Video/Source", PW_KEY_MEDIA_ROLE, "Screen", nint.Zero);
        if (props == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire properties.");
        }

        // Initialize callbacks.
        _processDelegate = ProcessCallback;
        _stateChangedDelegate = StreamStateChanged;

        // Prepare the stream events structure.
        var events = new pw_stream_events
        {
            version = PW_VERSION_STREAM_EVENTS,
            stateChanged = Marshal.GetFunctionPointerForDelegate(_stateChangedDelegate),
            process = Marshal.GetFunctionPointerForDelegate(_processDelegate),
        };

        _stream = pw_stream_new_simple(pw_main_loop_get_loop(_mainLoop), "screen-capture", props, ref events, nint.Zero);
        if (_stream == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire stream.");
        }

        // Build the PODs for format and buffer parameters.
        var formatPod = BuildVideoFormatPod((uint)_width, (uint)_height);
        var bufferPod = BuildBufferParamPod(4, 1, (uint)_frameSize, (uint)(_width * _bytesPerPixel), 4096, 1 << 0, 0);

        var pods = new[] { formatPod, bufferPod };
        var podCount = pods.Length;

        var paramArray = Marshal.AllocHGlobal(podCount * nint.Size);
        Marshal.Copy(pods, 0, paramArray, podCount);

        // Connect the stream. For screen capture, we use PW_DIRECTION_INPUT.
        var ret = pw_stream_connect(_stream, PW_DIRECTION_INPUT, PW_ID_ANY, PW_STREAM_FLAG_AUTOCONNECT, paramArray, (uint)podCount);
        if (ret < 0)
        {
            throw new Exception("Failed to connect PipeWire stream.");
        }

        // Run the PipeWire main loop in a separate thread.
        _pipeWireThread = new Thread(() =>
        {
            pw_main_loop_run(_mainLoop);
        })
        {
            IsBackground = true
        };

        _pipeWireThread.Start();
    }

    // Process callback: dequeues a buffer, extracts the frame, and queues the buffer back.
    private void ProcessCallback(nint userData)
    {
        var bufferPtr = pw_stream_dequeue_buffer(_stream);
        if (bufferPtr == nint.Zero)
        {
            return;
        }

        try
        {
            var frameData = ExtractFrameData(bufferPtr, _frameSize);
            _frameQueue.Enqueue(frameData);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in PipeWire process callback: {Message}", ex.Message);
        }
        finally
        {
            pw_stream_queue_buffer(_stream, bufferPtr);
        }
    }

    // State changed callback: logs state transitions.
    private void StreamStateChanged(nint userData, int oldState, int newState, string error)
    {
        _logger.LogInformation("Stream state changed: {OldState} -> {NewState}, error: {Error}", oldState, newState, error);
        // For example, assume:
        // 1 = UNCONNECTED, 2 = PAUSED, 3 = STREAMING.
        if (newState == 2 || newState == 3)
        {
            _logger.LogInformation("Negotiation complete. Stream is ready for data exchange.");
            // If necessary, you can update parameters here with pw_stream_update_params().
        }
    }

    /// <summary>
    /// Starts streaming screen data to the specified viewer.
    /// </summary>
    public void StartStreaming(IViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        _logger.LogInformation("Starting Linux PipeWire screen streaming for connection ID {ConnectionId}, User: {UserName}", viewer.ConnectionId, viewer.UserName);

        Task.Run(async () =>
        {
            while (!viewer.CancellationTokenSource.Token.IsCancellationRequested)
            {
                var delay = 1000 / viewer.CapturingContext.FrameRate;

                if (_frameQueue.TryDequeue(out var frame))
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
            pw_main_loop_quit(_mainLoop);
        }

        _pipeWireThread?.Join(2000);

        if (_stream != nint.Zero)
        {
            pw_stream_disconnect(_stream);
            pw_stream_destroy(_stream);
        }

        if (_mainLoop != nint.Zero)
        {
            pw_main_loop_destroy(_mainLoop);
        }
    }
}
