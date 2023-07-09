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
            let fullImageData = _buffer.flat();
            let blob = new Blob([new Uint8Array(fullImageData)], { type: 'image/png' });
            let url = URL.createObjectURL(blob);
            dotnetHelper.invokeMethodAsync('UpdateScreenDataUrl', url);
            _buffer = [];
        }
        else {
            _buffer.push(Array.from(dto.Data));
        }
    });

    connection.start().catch((err: Error) => console.error(err.toString()));
}

