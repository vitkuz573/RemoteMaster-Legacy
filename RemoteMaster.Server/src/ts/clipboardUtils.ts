export async function copyTextToClipboard(text: string): Promise<void> {
    try {
        await navigator.clipboard.writeText(text);
        console.log("Text copied to clipboard:", text);
    } catch (err) {
        console.error("Failed to copy text to clipboard:", err);
        throw new Error("Unable to copy text to clipboard.");
    }
}
