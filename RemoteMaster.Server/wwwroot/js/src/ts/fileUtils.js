import { createObjectBlobUrl, revokeUrl } from './blobUtils.js';
function convertBase64ToBlob(b64Data, contentType = '', sliceSize = 512) {
    const byteCharacters = atob(b64Data);
    const byteArrays = [];
    for (let offset = 0; offset < byteCharacters.length; offset += sliceSize) {
        const slice = byteCharacters.slice(offset, offset + sliceSize);
        const byteNumbers = new Array(slice.length).fill(null).map((_, index) => slice.charCodeAt(index));
        const byteArray = new Uint8Array(byteNumbers);
        byteArrays.push(byteArray);
    }
    return new Blob(byteArrays, { type: contentType });
}
function initiateDownload(blob, filename, contentType) {
    const url = createObjectBlobUrl(blob, contentType);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    revokeUrl(url);
}
export function downloadDataAsFile(data, filename, contentType) {
    let blob;
    if (typeof data === 'string' && contentType === 'application/json') {
        blob = new Blob([data], { type: contentType });
    }
    else if (typeof data === 'string' && contentType.includes('base64')) {
        blob = convertBase64ToBlob(data, contentType);
    }
    else if (typeof data === 'object') {
        const fileContent = JSON.stringify(data, null, 2);
        blob = new Blob([fileContent], { type: 'application/json' });
    }
    else {
        throw new Error('Unsupported data type for download.');
    }
    initiateDownload(blob, filename, contentType);
}
//# sourceMappingURL=fileUtils.js.map