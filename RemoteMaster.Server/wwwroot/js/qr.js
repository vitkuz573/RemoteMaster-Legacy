window.addEventListener("load", () => {
    const qrCodeElement = document.getElementById("qrCode");
    if (qrCodeElement) {
        const uri = document.getElementById("qrCodeData").getAttribute('data-url');
        new QRCode(qrCodeElement, {
            text: uri,
            width: 150,
            height: 150
        });
    }
});