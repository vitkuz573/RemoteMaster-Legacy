export function addKeyDownEventListener(dotNetHelper: DotNet.DotNetObject): void {
    window.onkeydown = (e: KeyboardEvent) => {
        dotNetHelper.invokeMethodAsync('OnKeyDown', e.code);
    };
}

export function addKeyUpEventListener(dotNetHelper: DotNet.DotNetObject): void {
    window.onkeyup = (e: KeyboardEvent) => {
        dotNetHelper.invokeMethodAsync('OnKeyUp', e.code);
    };
}

export function addPreventCtrlSListener(): void {
    window.addEventListener('keydown', (e: KeyboardEvent) => {
        if (e.ctrlKey && e.key === 's') {
            e.preventDefault();
        }
    });
}

export function addBeforeUnloadListener(instance: DotNet.DotNetObject): void {
    window.addEventListener('beforeunload', () => {
        instance.invokeMethodAsync('OnBeforeUnload');
    });
}

export function preventDefaultForKeydownWhenDrawerClosed(drawerOpen: boolean): void {
    document.addEventListener("keydown", (event: KeyboardEvent) => {
        if ((event.key === "Enter" || event.key === " " || event.key === "Spacebar") && !drawerOpen) {
            event.preventDefault();
        }
    });
}
