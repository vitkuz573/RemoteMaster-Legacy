export function addKeyDownEventListener(dotNetHelper) {
    window.onkeydown = (e) => {
        dotNetHelper.invokeMethodAsync('OnKeyDown', e.code);
    };
}
export function addKeyUpEventListener(dotNetHelper) {
    window.onkeyup = (e) => {
        dotNetHelper.invokeMethodAsync('OnKeyUp', e.code);
    };
}
export function addPreventCtrlSListener() {
    window.addEventListener('keydown', (e) => {
        if (e.ctrlKey && e.key === 's') {
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