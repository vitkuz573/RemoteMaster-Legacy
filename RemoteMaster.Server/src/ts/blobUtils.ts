export function revokeUrl(url: string): void {
    URL.revokeObjectURL(url);
}

export function createObjectBlobUrl(data: any, type: string): string {
    return URL.createObjectURL(new Blob([data], { type }));
}

export function createImageBlobUrl(data: any): string {
    return createObjectBlobUrl(data, 'image/jpeg');
}

export function getElementAttribute(element: HTMLElement, attribute: string): string | null {
    return element.getAttribute(attribute);
}

export function getElementRect(element: HTMLElement): DOMRect {
    return element.getBoundingClientRect();
}
