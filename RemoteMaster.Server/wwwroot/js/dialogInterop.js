window.addGlobalKeydownListener = function (dotNetHelper) {
    window.addEventListener('keydown', function (event) {
        dotNetHelper.invokeMethodAsync('HandleKeyDown', event.key);
    });
}