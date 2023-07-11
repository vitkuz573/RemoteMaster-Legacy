using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private readonly IScreenCasterService _streamingService;
    private readonly IViewerService _viewerService;
    private readonly ILogger<ControlHub> _logger;
    
    public ControlHub(ILogger<ControlHub> logger, IScreenCasterService streamingService, IViewerService viewerService)
    {
        _logger = logger;
        _streamingService = streamingService;
        _viewerService = viewerService;
    }

    public override async Task OnConnectedAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        Task.Run(() => _streamingService.StartStreaming(Context.ConnectionId, cancellationTokenSource.Token));
    }

    public async Task SetQuality(int quality)
    {
        _logger.LogInformation("Invoked SetQuality");
        
        _viewerService.SetImageQuality(quality);
    }

    public async Task SendMessage(string user, string message)
    {
        Console.WriteLine($"Received message {message} from {user}");
    }
}
