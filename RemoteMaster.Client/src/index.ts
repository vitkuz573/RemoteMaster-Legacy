import * as signalR from "@microsoft/signalr";
import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";

declare global {
    interface Window {
        setupSignalRConnection: (host: string) => void;
    }
}

window.setupSignalRConnection = function (host: string) {
    let connection = new signalR.HubConnectionBuilder()
        .withUrl(`http://${host}:5076/hubs/control`, {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withHubProtocol(new MessagePackHubProtocol())
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ScreenUpdate", (data) => {
        console.log(data);
    });

    connection.start().catch(err => console.error(err.toString()));
}
