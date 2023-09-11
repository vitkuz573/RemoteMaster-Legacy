using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Updater.Common;

namespace RemoteMaster.Client.Services;

public class AgentUpdater : UpdaterBase, IAgentUpdater
{
    private readonly IServiceManager _serviceManager;

    private const string Folder = $"{SharedFolder}/Agent";

    public AgentUpdater(IServiceManager serviceManager, ILogger<AgentUpdater> logger) : base(logger)
    {
        _serviceManager = serviceManager;
    }

    public void Update()
    {
        _serviceManager.StopService();

        Thread.Sleep(30000);

        MapNetworkDrive(SharedFolder, Login, Password);
        DirectoryCopy(Folder, $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster/Agent", true, true);
        CancelNetworkDrive(SharedFolder);

        _serviceManager.StartService();
    }
}
