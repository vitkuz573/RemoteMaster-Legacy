using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
    public async Task UpdateScreenDataUrl(string url)
    {
        _screenDataUrl = url;
        await InvokeAsync(StateHasChanged);
    }

    public async Task QualityChanged(ChangeEventArgs e)
    {
        var quality = int.Parse(e.Value.ToString());
        await JSRuntime.InvokeVoidAsync("window.setQuality", quality);
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        await JSRuntime.InvokeVoidAsync("window.sendMouseCoordinates", e.ClientX, e.ClientY);
    }
}
