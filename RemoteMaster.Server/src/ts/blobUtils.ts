export function revokeUrl(url: string): void {
    URL.revokeObjectURL(url);
}

export function createObjectBlobUrl(data: BlobPart, type: string): string {
    return URL.createObjectURL(new Blob([data], { type }));
}

export function createImageBlobUrl(data: BlobPart): string {
    return createObjectBlobUrl(data, 'image/jpeg');
}

export function getElementAttribute(element: HTMLElement, attribute: string): string | null {
    return element.getAttribute(attribute);
}

export function getElementRect(element: HTMLElement): DOMRect {
    return element.getBoundingClientRect();
}
