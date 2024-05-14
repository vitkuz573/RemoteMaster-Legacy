export function openNewWindow(url: string, width: number, height: number): Window | null {
    const windowFeatures = `toolbar=no,location=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=${width},height=${height}`;
    return window.open(url, '_blank', windowFeatures);
}