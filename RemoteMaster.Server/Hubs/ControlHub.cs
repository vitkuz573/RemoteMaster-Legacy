using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _connectionCancellationTokens;
    private readonly IStreamingService _streamingService;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(ILogger<ControlHub> logger, IStreamingService streamingService)
    {
        _logger = logger;
        _streamingService = streamingService;
        _connectionCancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    public override async Task OnConnectedAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        _connectionCancellationTokens.TryAdd(Context.ConnectionId, cancellationTokenSource);

        await _streamingService.StartStreaming(Context.ConnectionId, cancellationTokenSource.Token);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionCancellationTokens.TryRemove(Context.ConnectionId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
        }
    }
}
