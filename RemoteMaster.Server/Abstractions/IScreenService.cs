using System.Drawing;

namespace RemoteMaster.Server.Abstractions;

public interface IScreenService
{
    Size GetScreenSize();

    Size GetVirtualScreenSize();
}
