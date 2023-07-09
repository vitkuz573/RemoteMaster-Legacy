import { HubConnectionBuilder, LogLevel, HttpTransportType } from "@microsoft/signalr";
import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";

declare global {
    interface Window {
        setupSignalRConnection: (host: string, dotnetHelper: any) => void;
    }
}

let _buffer: number[][] = [];

window.setupSignalRConnection = function (host: string, dotnetHelper: any) {
    let connection = new HubConnectionBuilder()
        .withUrl(`http://${host}:5076/hubs/control`, {
            skipNegotiation: true,
            transport: HttpTransportType.WebSockets
        })
        .withHubProtocol(new MessagePackHubProtocol())
        .configureLogging(LogLevel.Information)
        .build();

    connection.on("ScreenUpdate", (dto: { Data: Uint8Array, IsEndOfImage: boolean }) => {
        if (dto.IsEndOfImage) {
            let fullImageData = _buffer.reduce((acc, val) => acc.concat(val), []);
            let binary = String.fromCharCode(...new Uint8Array(fullImageData));
            let dataUrl = "data:image/png;base64," + window.btoa(binary);
            dotnetHelper.invokeMethodAsync('UpdateScreenDataUrl', dataUrl);
            _buffer = [];
        }
        else {
            _buffer.push(Array.from(dto.Data));
        }
    });

    connection.start().catch(err => console.error(err.toString()));
}
