using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Core.Abstractions;

namespace RemoteMaster.Agent.Core.Hubs;

public class MainHub : Hub
{
    private readonly ISignatureService _signatureService;
    private readonly IProcessService _processService;
    private readonly ILogger<MainHub> _logger;

    private const string ClientPath = "C:\\Program Files\\RemoteMaster\\Client\\RemoteMaster.Client.exe";
    private const string CertificateThumbprint = "E0BD3A7C39AA4FC012A0F6CB3297B46D5D73210C";

    public MainHub(ISignatureService signatureService, IProcessService processService, ILogger<MainHub> logger)
    {
        _signatureService = signatureService;
        _processService = processService;
        _logger = logger;
    }

    public async override Task OnConnectedAsync()
    {
        if (!IsClientRunning())
        {
            if (_signatureService.IsSignatureValid(ClientPath, CertificateThumbprint))
            {
                try
                {
                    _processService.Start(ClientPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting RemoteMaster Client");
                }
            }
            else
            {
                _logger.LogInformation("Sending ClientTampered message to client");
                await Clients.Client(Context.ConnectionId).SendAsync("ClientTampered", $"The RemoteMaster client appears to be tampered with or its digital signature is not valid. Please contact support. (Expected Thumbprint: {CertificateThumbprint})");
            }
        }

        await base.OnConnectedAsync();
    }

    private bool IsClientRunning()
    {
        var clientFullPath = Path.GetFullPath(ClientPath);

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (_signatureService.IsProcessSignatureValid(process, clientFullPath, CertificateThumbprint))
                {
                    return true;
                }
                else if (process.MainModule != null && string.Equals(Path.GetFullPath(process.MainModule.FileName), clientFullPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning($"Detected a process with the same name as the client but different signature. Killing process ID: {process.Id}");
                    process.Kill();
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Unable to enumerate the process modules for process ID: {process.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occurred when handling process ID: {process.Id}");
            }
        }

        return false;
    }
}
