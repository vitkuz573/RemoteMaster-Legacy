import { createObjectBlobUrl, revokeUrl } from './blobUtils.js';

function b64toBlob(b64Data: string, contentType = '', sliceSize = 512): Blob {
    const byteCharacters = atob(b64Data);
    const byteArrays: Uint8Array[] = [];

    for (let offset = 0; offset < byteCharacters.length; offset += sliceSize) {
        const slice = byteCharacters.slice(offset, offset + sliceSize);

        const byteNumbers = new Array(slice.length);
        for (let i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i);
        }

        const byteArray = new Uint8Array(byteNumbers);
        byteArrays.push(byteArray);
    }

    return new Blob(byteArrays, { type: contentType });
}

export function generateAndDownloadFile(requestData: any): void {
    const fileContent = JSON.stringify(requestData, null, 2);
    const blob = new Blob([fileContent], { type: 'application/json' });
    const url = createObjectBlobUrl(blob, 'application/json');

    const a = document.createElement('a');
    a.href = url;
    a.download = 'RemoteMaster.Host.json';
    document.body.appendChild(a);
    a.click();
    a.remove();

    revokeUrl(url);
}

export function generateAndDownloadResults(base64Data: string, fileName: string): void {
    const blob = b64toBlob(base64Data, 'application/zip');
    const url = createObjectBlobUrl(blob, 'application/zip');

    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();

    revokeUrl(url);
}

export function saveAsFile(filename: string, bytesBase64: string): void {
    const blob = b64toBlob(bytesBase64, 'application/octet-stream');
    const url = createObjectBlobUrl(blob, 'application/octet-stream');

    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();

    revokeUrl(url);
}