using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Abstractions;

public interface IScreenCaster
{
    Task StartStreaming(string connectionId, CancellationToken cancellationToken);

    void SetSelectedScreen(string connectionId, SelectScreenDto dto);
}
