export function trackSelectedElements(containerId, selectableSelector, selectionStyles, dotNetHelper) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }
    let startPoint = null;
    let selectionRect = null;
    let isCtrlPressed = false;
    const createRectFromPoints = (p1, p2) => {
        const left = Math.min(p1.x, p2.x);
        const top = Math.min(p1.y, p2.y);
        const width = Math.abs(p1.x - p2.x);
        const height = Math.abs(p1.y - p2.y);
        return new DOMRect(left, top, width, height);
    };
    const rectOverlap = (rect1, rect2) => {
        return !(rect1.right < rect2.left || rect1.left > rect2.right || rect1.bottom < rect2.top || rect1.top > rect2.bottom);
    };
    const hasAllSelectionStyles = (element) => {
        return selectionStyles.every(style => element.classList.contains(style));
    };
    const updateElementSelection = () => {
        const elements = container.querySelectorAll(selectableSelector);
        elements.forEach(element => {
            const elemRect = element.getBoundingClientRect();
            if (selectionRect && rectOverlap(selectionRect, elemRect)) {
                toggleSelection(element, !hasAllSelectionStyles(element));
            }
            else if (!isCtrlPressed) {
                element.classList.remove(...selectionStyles);
            }
        });
    };
    const toggleSelection = (element, add) => {
        if (add) {
            element.classList.add(...selectionStyles);
        }
        else if (isCtrlPressed) {
            element.classList.remove(...selectionStyles);
        }
    };
    const onMove = ({ clientX, clientY }) => {
        if (!startPoint) {
            return;
        }
        selectionRect = createRectFromPoints(startPoint, new DOMPoint(clientX, clientY));
        updateElementSelection();
    };
    const onComplete = ({ clientX, clientY }) => {
        if (startPoint) {
            selectionRect = createRectFromPoints(startPoint, new DOMPoint(clientX, clientY));
            updateElementSelection();
            dotNetHelper.invokeMethodAsync('UpdateSelectedElements', Array.from(container.querySelectorAll(selectableSelector))
                .filter(element => hasAllSelectionStyles(element))
                .map(el => el.id));
            if (!isCtrlPressed) {
                startPoint = null;
                selectionRect = null;
            }
        }
    };
    const onMouseDown = (e) => {
        isCtrlPressed = e.ctrlKey;
        startPoint = new DOMPoint(e.clientX, e.clientY);
        container.addEventListener('mousemove', onMouseMove);
    };
    const onMouseMove = (e) => onMove(e);
    const onMouseUp = (e) => {
        container.removeEventListener('mousemove', onMouseMove);
        onComplete(e);
    };
    const onTouchStart = (e) => {
        isCtrlPressed = e.touches.length > 1;
        startPoint = new DOMPoint(e.touches[0].clientX, e.touches[0].clientY);
        container.addEventListener('touchmove', onTouchMove);
    };
    const onTouchMove = (e) => {
        if (e.touches.length > 0) {
            onMove(e.touches[0]);
        }
    };
    const onTouchEnd = (e) => {
        if (e.changedTouches.length > 0) {
            container.removeEventListener('touchmove', onTouchMove);
            onComplete(e.changedTouches[0]);
        }
    };
    const eventListeners = [
        { type: 'mousedown', listener: onMouseDown },
        { type: 'mouseup', listener: onMouseUp },
        { type: 'touchstart', listener: onTouchStart },
        { type: 'touchend', listener: onTouchEnd }
    ];
    eventListeners.forEach(({ type, listener }) => container.addEventListener(type, listener));
}
//# sourceMappingURL=SelectableArea.js.map