import { HubConnectionBuilder, LogLevel, HttpTransportType } from "@microsoft/signalr";
import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";

declare global {
    interface Window {
        setupSignalRConnection: (host: string, dotnetHelper: any) => void;
    }
}

let _buffer: Uint8Array[] = [];

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
        _buffer.push(dto.Data);

        if (dto.IsEndOfImage) {
            let blob = new Blob(_buffer, { type: 'image/png' });
            let url = URL.createObjectURL(blob);
            dotnetHelper.invokeMethodAsync('UpdateScreenDataUrl', url);
            _buffer = [];
        }
    });

    connection.start().catch((err: Error) => console.error(err.toString()));
}
