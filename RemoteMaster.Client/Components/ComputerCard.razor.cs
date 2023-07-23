using Microsoft.AspNetCore.Components;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Components;

public partial class ComputerCard : ComponentBase
{
    [Parameter]
    public Computer Computer { get; set; }

    [Parameter]
    public EventCallback<Computer> OnOpenShell { get; set; }

    [Parameter]
    public EventCallback<Computer> OnOpenInNewTab { get; set; }
}
