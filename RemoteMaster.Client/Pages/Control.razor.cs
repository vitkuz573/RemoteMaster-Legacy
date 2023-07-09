using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RemoteMaster.Client.Pages;

public partial class Control
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("window.setupSignalRConnection", Host);
        }
    }
}
