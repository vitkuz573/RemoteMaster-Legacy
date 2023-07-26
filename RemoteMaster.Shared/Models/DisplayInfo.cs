using System.Drawing;

namespace RemoteMaster.Shared.Models;

public class DisplayInfo
{
    public string Name { get; set; }

    public bool IsPrimary { get; set; }

    public Size Resolution { get; set; }
}
