document.addEventListener('DOMContentLoaded', function () {
    const targetNode = document.querySelector('body');
    const config = { childList: true, subtree: true };
    const callback = function (mutationsList, observer) {
        for (let mutation of mutationsList) {
            if (mutation.type === 'childList') {
                const qrCodeElement = document.getElementById("qrCode");
                if (qrCodeElement && !qrCodeElement.hasChildNodes()) {
                    const uri = document.getElementById("qrCodeData").getAttribute('data-url');
                    new QRCode(qrCodeElement, {
                        text: uri,
                        width: 150,
                        height: 150
                    });
                }
            }
        }
    };
    const observer = new MutationObserver(callback);
    observer.observe(targetNode, config);
});
