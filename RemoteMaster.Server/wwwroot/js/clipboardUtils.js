export async function copyTextToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        console.log("Text copied to clipboard:", text);
    }
    catch (err) {
        console.error("Failed to copy text to clipboard:", err);
        throw new Error("Unable to copy text to clipboard.");
    }
}
//# sourceMappingURL=clipboardUtils.js.map