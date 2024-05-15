export function openNewWindow(url, width, height) {
    const windowFeatures = `toolbar=no,location=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=${width},height=${height}`;
    return window.open(url, '_blank', windowFeatures);
}
//# sourceMappingURL=windowOperations.js.map