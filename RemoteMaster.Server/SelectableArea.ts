export function trackSelectedElements(containerId: string, selectableSelector: string, selectionStyles: string[], dotNetHelper: DotNet.DotNetObject): void {
    const container = document.getElementById(containerId) as HTMLElement;

    if (!container) {
        return;
    }

    let startPoint: DOMPoint | null = null;
    let selectionRect: DOMRect | null = null;
    let isCtrlPressed = false;

    const createRectFromPoints = (p1: DOMPoint, p2: DOMPoint): DOMRect => {
        const left = Math.min(p1.x, p2.x);
        const top = Math.min(p1.y, p2.y);
        const width = Math.abs(p1.x - p2.x);
        const height = Math.abs(p1.y - p2.y);

        return new DOMRect(left, top, width, height);
    };

    const rectOverlap = (rect1: DOMRect, rect2: DOMRect): boolean => {
        return !(rect1.right < rect2.left || rect1.left > rect2.right || rect1.bottom < rect2.top || rect1.top > rect2.bottom);
    };

    const hasAllSelectionStyles = (element: HTMLElement): boolean => {
        return selectionStyles.every(style => element.classList.contains(style));
    };

    const updateElementSelection = (): void => {
        const elements = container.querySelectorAll(selectableSelector) as NodeListOf<HTMLElement>;

        elements.forEach(element => {
            const elemRect = element.getBoundingClientRect();

            if (selectionRect && rectOverlap(selectionRect, elemRect)) {
                toggleSelection(element, !hasAllSelectionStyles(element));
            } else if (!isCtrlPressed) {
                element.classList.remove(...selectionStyles);
            }
        });
    };

    const toggleSelection = (element: HTMLElement, add: boolean): void => {
        if (add) {
            element.classList.add(...selectionStyles);
        } else if (isCtrlPressed) {
            element.classList.remove(...selectionStyles);
        }
    };

    const onMove = ({ clientX, clientY }: MouseEvent | Touch): void => {
        if (!startPoint) {
            return;
        }

        selectionRect = createRectFromPoints(startPoint, new DOMPoint(clientX, clientY));
        updateElementSelection();
    };

    const onComplete = ({ clientX, clientY }: MouseEvent | Touch): void => {
        if (startPoint) {
            selectionRect = createRectFromPoints(startPoint, new DOMPoint(clientX, clientY));
            updateElementSelection();
            dotNetHelper.invokeMethodAsync('UpdateSelectedElements',
                Array.from(container.querySelectorAll(selectableSelector) as NodeListOf<HTMLElement>)
                    .filter(element => hasAllSelectionStyles(element))
                    .map(el => el.id)
            );

            if (!isCtrlPressed) {
                startPoint = null;
                selectionRect = null;
            }
        }
    };

    const onMouseDown = (e: MouseEvent): void => {
        isCtrlPressed = e.ctrlKey;
        startPoint = new DOMPoint(e.clientX, e.clientY);
        container.addEventListener('mousemove', onMouseMove as EventListener);
    };

    const onMouseMove = (e: MouseEvent): void => onMove(e);

    const onMouseUp = (e: MouseEvent): void => {
        container.removeEventListener('mousemove', onMouseMove as EventListener);
        onComplete(e);
    };

    const onTouchStart = (e: TouchEvent): void => {
        isCtrlPressed = e.touches.length > 1;
        startPoint = new DOMPoint(e.touches[0].clientX, e.touches[0].clientY);
        container.addEventListener('touchmove', onTouchMove as EventListener);
    };

    const onTouchMove = (e: TouchEvent): void => {
        if (e.touches.length > 0) {
            onMove(e.touches[0]);
        }
    };

    const onTouchEnd = (e: TouchEvent): void => {
        if (e.changedTouches.length > 0) {
            container.removeEventListener('touchmove', onTouchMove as EventListener);
            onComplete(e.changedTouches[0]);
        }
    };

    const eventListeners: { type: keyof HTMLElementEventMap, listener: EventListener }[] = [
        { type: 'mousedown', listener: onMouseDown as EventListener },
        { type: 'mouseup', listener: onMouseUp as EventListener },
        { type: 'touchstart', listener: onTouchStart as EventListener },
        { type: 'touchend', listener: onTouchEnd as EventListener }
    ];

    eventListeners.forEach(({ type, listener }) => container.addEventListener(type, listener));
}
