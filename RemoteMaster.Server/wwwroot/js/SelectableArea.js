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
    function onMove(clientX, clientY) {
        if (!startPoint) {
            return;
        }
        selectionRect = createRectFromPoints(new DOMPoint(startPoint.x, startPoint.y), new DOMPoint(clientX, clientY));
        updateElementSelection();
    }
    function onComplete(clientX, clientY) {
        if (startPoint) {
            selectionRect = createRectFromPoints(new DOMPoint(startPoint.x, startPoint.y), new DOMPoint(clientX, clientY));
            updateElementSelection();
            dotNetHelper.invokeMethodAsync('UpdateSelectedElements', Array.from(container.querySelectorAll(selectableSelector))
                .filter(hasAllSelectionStyles)
                .map(el => el.id));
            if (!isCtrlPressed) {
                startPoint = null;
                selectionRect = null;
            }
        }
    }
    function onMouseDown(e) {
        isCtrlPressed = e.ctrlKey;
        startPoint = new DOMPoint(e.clientX, e.clientY);
        container.addEventListener('mousemove', onMouseMove);
    }
    function onMouseMove(e) {
        onMove(e.clientX, e.clientY);
    }
    function onMouseUp(e) {
        container.removeEventListener('mousemove', onMouseMove);
        onComplete(e.clientX, e.clientY);
    }
    function onTouchStart(e) {
        isCtrlPressed = e.touches.length > 1;
        startPoint = new DOMPoint(e.touches[0].clientX, e.touches[0].clientY);
        container.addEventListener('touchmove', onTouchMove);
    }
    function onTouchMove(e) {
        if (e.touches.length > 0) {
            onMove(e.touches[0].clientX, e.touches[0].clientY);
        }
    }
    function onTouchEnd(e) {
        if (e.changedTouches.length > 0) {
            container.removeEventListener('touchmove', onTouchMove);
            onComplete(e.changedTouches[0].clientX, e.changedTouches[0].clientY);
        }
    }
    container.addEventListener('mousedown', onMouseDown);
    container.addEventListener('mouseup', onMouseUp);
    container.addEventListener('touchstart', onTouchStart);
    container.addEventListener('touchend', onTouchEnd);
}
//# sourceMappingURL=SelectableArea.js.map