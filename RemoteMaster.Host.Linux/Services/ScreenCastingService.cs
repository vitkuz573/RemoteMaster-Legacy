// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
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

    private readonly int _width = 1920;
    private readonly int _height = 1080;
    private readonly int _bytesPerPixel = 3; // For RGB24.
    private readonly int _frameSize; // Calculated as width * height * bytesPerPixel.

    public ScreenCastingService(IHubContext<ControlHub, IControlClient> hubContext, ILogger<ScreenCastingService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _frameSize = _width * _height * _bytesPerPixel;
        InitializePipeWire();
    }

    private void InitializePipeWire()
    {
        var argc = 0;
        var argv = IntPtr.Zero;
        
        pw_init(ref argc, ref argv);

        _mainLoop = pw_main_loop_new(nint.Zero);

        if (_mainLoop == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire main loop.");
        }

        var props = pw_properties_new(PW_KEY_MEDIA_TYPE, "Video", PW_KEY_MEDIA_CATEGORY, "Source", PW_KEY_MEDIA_ROLE, "Screen", IntPtr.Zero);

        if (props == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire properties.");
        }

        pw_properties_set(props, "media.role", "Screen");

        var events = new pw_stream_events
        {
            version = PW_VERSION_STREAM_EVENTS,
            //process = Marshal.GetFunctionPointerForDelegate(new PwStreamProcessDelegate(ProcessCallback))
        };

        _stream = pw_stream_new_simple(pw_main_loop_get_loop(_mainLoop), "screen-capture", props, ref events, nint.Zero);

        if (_stream == nint.Zero)
        {
            throw new Exception("Failed to create PipeWire stream.");
        }

        var bufferPod = BuildBufferParamPod(4, 1, (uint)_frameSize, (uint)(_width * _bytesPerPixel), 4096, 1 << 0, 0);

        var parameters = new[] { bufferPod };
        var paramArray = Marshal.AllocHGlobal(1 * nint.Size);

        Marshal.Copy(parameters, 0, paramArray, 1);

        var ret = pw_stream_connect(_stream, PW_DIRECTION_INPUT, PW_ID_ANY, PW_STREAM_FLAG_AUTOCONNECT, paramArray, 1);

        if (ret < 0)
        {
            throw new Exception("Failed to connect PipeWire stream.");
        }

        _pipeWireThread = new Thread(() =>
        {
            pw_main_loop_run(_mainLoop);
        })
        {
            IsBackground = true
        };

        _pipeWireThread.Start();
    }

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

    /// <summary>
    /// Starts streaming screen data to the specified viewer.
    /// </summary>
    public void StartStreaming(IViewer viewer)
    {
        if (viewer == null)
        {
            throw new ArgumentNullException(nameof(viewer));
        }

        _logger.LogInformation("Starting Linux PipeWire screen streaming for connection ID {ConnectionId}, User: {UserName}", viewer.ConnectionId, viewer.UserName);

        Task.Run(async () =>
        {
            while (!viewer.CancellationTokenSource.Token.IsCancellationRequested)
            {
                var delay = 1000 / viewer.CapturingContext.FrameRate;
                
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

        // Free any additional allocated memory (such as the parameter array) as needed.
    }
}
