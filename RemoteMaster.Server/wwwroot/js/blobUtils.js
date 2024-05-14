export function revokeUrl(url) {
    URL.revokeObjectURL(url);
}
export function createObjectBlobUrl(data, type) {
    return URL.createObjectURL(new Blob([data], { type }));
}
export function createImageBlobUrl(data) {
    return createObjectBlobUrl(data, 'image/jpeg');
}
export function getElementAttribute(element, attribute) {
    return element.getAttribute(attribute);
}
//# sourceMappingURL=blobUtils.js.map