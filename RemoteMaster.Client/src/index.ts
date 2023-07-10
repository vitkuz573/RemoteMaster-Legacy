import { HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'

declare global {
  interface Window {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    setupSignalRConnection: (host: string, dotnetHelper: any) => void
    setQuality: (quality: number) => void
  }
}

interface ScreenUpdateDto {
  Data: Uint8Array
  IsEndOfImage: boolean
}

let _buffer: Uint8Array[] = []
// eslint-disable-next-line @typescript-eslint/no-explicit-any
let connection: any

// eslint-disable-next-line @typescript-eslint/no-explicit-any
window.setupSignalRConnection = function (host: string, dotnetHelper: any) {
  connection = new HubConnectionBuilder()
    .withUrl(`http://${host}:5076/hubs/control`, {
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets
    })
    .withAutomaticReconnect([0, 3000, 5000, 10000, 15000, 30000])
    .withHubProtocol(new MessagePackHubProtocol())
    .configureLogging(LogLevel.Information)
    .build()

  connection.on('ScreenUpdate', (dto: ScreenUpdateDto) => {
    _buffer.push(dto.Data)

    if (dto.IsEndOfImage) {
      const blob = new Blob(_buffer, { type: 'image/jpeg' })
      const url = URL.createObjectURL(blob)
      dotnetHelper.invokeMethodAsync('UpdateScreenDataUrl', url)
      _buffer = []
    }
  })

  connection.start().catch((err: Error) => { console.error(err.toString()) })
}

window.setQuality = function (quality) {
  console.log('SetQuality on client Invoked')
  console.log('Connection state: ' + connection.state)

  connection.invoke('SetQuality', quality)
}
