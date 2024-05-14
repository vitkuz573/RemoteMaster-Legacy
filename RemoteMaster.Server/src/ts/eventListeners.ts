export function addKeyDownEventListener(dotnetHelper: any): void {
    window.onkeydown = (e: KeyboardEvent) => {
        dotnetHelper.invokeMethodAsync('OnKeyDown', e.keyCode);
    };
}

export function addKeyUpEventListener(dotnetHelper: any): void {
    window.onkeyup = (e: KeyboardEvent) => {
        dotnetHelper.invokeMethodAsync('OnKeyUp', e.keyCode);
    };
}

export function addPreventCtrlSListener(): void {
    window.addEventListener('keydown', (e: KeyboardEvent) => {
        if (e.ctrlKey && e.which === 83) {
            e.preventDefault();
        }
    });
}

export function addBeforeUnloadListener(instance: any): void {
    window.addEventListener('beforeunload', () => {
        instance.invokeMethodAsync('OnBeforeUnload');
    });
}