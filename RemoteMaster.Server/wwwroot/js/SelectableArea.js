export function trackSelectedElements(containerId, selectableSelector, selectionStyles, dotNetHelper) {
    const container = document.getElementById(containerId);
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
        return selectionStyles.every(style => element.classList.contains(style));
    }
    function updateElementSelection() {
        const elements = container.querySelectorAll(selectableSelector);
        elements.forEach(element => {
            const elemRect = element.getBoundingClientRect();
            if (selectionRect && rectOverlap(selectionRect, elemRect)) {
                if (!hasAllSelectionStyles(element)) {
                    element.classList.add(...selectionStyles);
                }
                else if (isCtrlPressed) {
                    element.classList.remove(...selectionStyles);
                }
            }
            else if (!isCtrlPressed) {
                element.classList.remove(...selectionStyles);
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
    function completeSelection(endPoint) {
        if (startPoint) {
            selectionRect = createRectFromPoints(new DOMPoint(startPoint.x, startPoint.y), endPoint);
            updateElementSelection();
            dotNetHelper.invokeMethodAsync('UpdateSelectedElements', Array.from(container.querySelectorAll(selectableSelector)).filter(hasAllSelectionStyles).map(el => el.id));
            if (!isCtrlPressed) {
                startPoint = null;
                selectionRect = null;
            }
        }
    }
    container.addEventListener('mousedown', (e) => {
        isCtrlPressed = e.ctrlKey;
        startPoint = new DOMPoint(e.clientX, e.clientY);
        container.addEventListener('mousemove', onMouseMove);
    });
    container.addEventListener('mouseup', (e) => {
        container.removeEventListener('mousemove', onMouseMove);
        completeSelection(new DOMPoint(e.clientX, e.clientY));
    });
    container.addEventListener('touchstart', (e) => {
        isCtrlPressed = e.touches.length > 1;
        startPoint = new DOMPoint(e.touches[0].clientX, e.touches[0].clientY);
        container.addEventListener('touchmove', onTouchMove);
    });
    container.addEventListener('touchend', (e) => {
        if (e.changedTouches && e.changedTouches.length > 0) {
            container.removeEventListener('touchmove', onTouchMove);
            completeSelection(new DOMPoint(e.changedTouches[0].clientX, e.changedTouches[0].clientY));
        }
    });
}
//# sourceMappingURL=SelectableArea.js.map