export function addKeyDownEventListener(dotnetHelper) {
    window.onkeydown = (e) => {
        dotnetHelper.invokeMethodAsync('OnKeyDown', e.keyCode);
    };
}
export function addKeyUpEventListener(dotnetHelper) {
    window.onkeyup = (e) => {
        dotnetHelper.invokeMethodAsync('OnKeyUp', e.keyCode);
    };
}
export function addPreventCtrlSListener() {
    window.addEventListener('keydown', (e) => {
        if (e.ctrlKey && e.which === 83) {
            e.preventDefault();
        }
    });
}
export function addBeforeUnloadListener(instance) {
    window.addEventListener('beforeunload', () => {
        instance.invokeMethodAsync('OnBeforeUnload');
    });
}
//# sourceMappingURL=eventListeners.js.map