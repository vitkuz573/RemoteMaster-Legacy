using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RemoteMaster.Client.Models;

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
        var imgElement = await JSRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<DOMRect>("getBoundingClientRect");

        // вычитаем позицию изображения из координат мыши
        var relativeX = e.ClientX - imgPosition.Left;
        var relativeY = e.ClientY - imgPosition.Top;

        var absoluteX = Math.Round(relativeX * 65535 / imgPosition.Width);
        var absoluteY = Math.Round(relativeY * 65535 / imgPosition.Height);

        await JSRuntime.InvokeVoidAsync("window.sendMouseCoordinates", absoluteX, absoluteY);
    }

    private async Task OnMouseClick(MouseEventArgs e)
    {
        var imgElement = await JSRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<DOMRect>("getBoundingClientRect");

        // вычитаем позицию изображения из координат мыши
        var relativeX = e.ClientX - imgPosition.Left;
        var relativeY = e.ClientY - imgPosition.Top;

        var absoluteX = Math.Round(relativeX * 65535 / imgPosition.Width);
        var absoluteY = Math.Round(relativeY * 65535 / imgPosition.Height);

        await JSRuntime.InvokeVoidAsync("window.sendMouseButton", e.Button, e.Type, absoluteX, absoluteY);
    }
}

