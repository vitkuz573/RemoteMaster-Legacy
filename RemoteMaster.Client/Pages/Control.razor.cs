using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RemoteMaster.Client.Pages;

public partial class Control
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    private string _screenDataUrl;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var dotnetHelper = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("window.setupSignalRConnection", Host, dotnetHelper);
        }
    }

    [JSInvokable]
    public async Task UpdateScreenDataUrl(string dataUrl)
    {
        _screenDataUrl = dataUrl;
        await InvokeAsync(StateHasChanged);
    }
}
