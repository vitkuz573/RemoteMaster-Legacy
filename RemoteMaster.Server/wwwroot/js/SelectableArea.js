export function trackSelectedElements(containerId, selectableSelector, dotNetHelper) {
    const container = document.getElementById(containerId);
    const SELECTION_STYLES = ['ring-2', 'ring-blue-500', 'shadow-lg'];
    if (!container) {
        return;
    }
    let startPoint = null;
    let selectionRect = null;
    let isCtrlPressed = false;
    function createRectFromPoints(p1, p2) {
        const left = Math.min(p1.x, p2.x);
        const top = Math.min(p1.y, p2.y);
        const width = Math.abs(p1.x - p2.x);
        const height = Math.abs(p1.y - p2.y);
        return new DOMRect(left, top, width, height);
    }
    function rectOverlap(rect1, rect2) {
        return !(rect1.right < rect2.left || rect1.left > rect2.right || rect1.bottom < rect2.top || rect1.top > rect2.bottom);
    }
    function hasAllSelectionStyles(element) {
        return SELECTION_STYLES.every(style => element.classList.contains(style));
    }
    function updateElementSelection() {
        const elements = container.querySelectorAll(selectableSelector);
        elements.forEach(element => {
            const elemRect = element.getBoundingClientRect();
            if (selectionRect && rectOverlap(selectionRect, elemRect)) {
                if (!hasAllSelectionStyles(element)) {
                    element.classList.add(...SELECTION_STYLES);
                }
                else if (isCtrlPressed) {
                    element.classList.remove(...SELECTION_STYLES);
                }
            }
            else if (!isCtrlPressed) {
                element.classList.remove(...SELECTION_STYLES);
            }
        });
    }
    function onMouseMove(e) {
        if (!startPoint) {
            return;
        }
        selectionRect = createRectFromPoints(new DOMPoint(startPoint.x, startPoint.y), new DOMPoint(e.clientX, e.clientY));
        updateElementSelection();
    }
    function onTouchMove(e) {
        if (!startPoint || e.touches.length === 0) {
            return;
        }
        selectionRect = createRectFromPoints(new DOMPoint(startPoint.x, startPoint.y), new DOMPoint(e.touches[0].clientX, e.touches[0].clientY));
        updateElementSelection();
    }
    container.addEventListener('mousedown', (e) => {
        isCtrlPressed = e.ctrlKey;
        startPoint = new DOMPoint(e.clientX, e.clientY);
        container.addEventListener('mousemove', onMouseMove);
    });
    container.addEventListener('mouseup', (e) => {
        if (startPoint) {
            container.removeEventListener('mousemove', onMouseMove);
            selectionRect = createRectFromPoints(new DOMPoint(startPoint.x, startPoint.y), new DOMPoint(e.clientX, e.clientY));
            updateElementSelection();
            dotNetHelper.invokeMethodAsync('UpdateSelectedElements', Array.from(container.querySelectorAll('.selectable')).filter(hasAllSelectionStyles).map(el => el.id));
            if (!isCtrlPressed) {
                startPoint = null;
                selectionRect = null;
            }
        }
    });
    container.addEventListener('touchstart', (e) => {
        isCtrlPressed = e.touches.length > 1;
        startPoint = new DOMPoint(e.touches[0].clientX, e.touches[0].clientY);
        container.addEventListener('touchmove', onTouchMove);
    });
    container.addEventListener('touchend', (e) => {
        if (e.changedTouches && e.changedTouches.length > 0 && startPoint) {
            container.removeEventListener('touchmove', onTouchMove);
            selectionRect = createRectFromPoints(new DOMPoint(startPoint.x, startPoint.y), new DOMPoint(e.changedTouches[0].clientX, e.changedTouches[0].clientY));
            updateElementSelection();
            dotNetHelper.invokeMethodAsync('UpdateSelectedElements', Array.from(container.querySelectorAll('.selectable')).filter(hasAllSelectionStyles).map(el => el.id));
        }
        if (!isCtrlPressed) {
            startPoint = null;
            selectionRect = null;
        }
    });
}
//# sourceMappingURL=SelectableArea.js.map