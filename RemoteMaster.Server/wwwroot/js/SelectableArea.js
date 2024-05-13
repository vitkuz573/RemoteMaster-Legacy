export function trackSelectedElements(containerId, dotNetHelper) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }
    let startPoint = null;
    let selectionRect = null;
    let isCtrlPressed = false;
    function createRectFromPoints(p1, p2) {
        const x = Math.min(p1.x, p2.x);
        const y = Math.min(p1.y, p2.y);
        const width = Math.abs(p1.x - p2.x);
        const height = Math.abs(p1.y - p2.y);
        return {
            left: x,
            top: y,
            right: x + width,
            bottom: y + height
        };
    }
    function rectOverlap(rect1, rect2) {
        return !(rect1.right < rect2.left || rect1.left > rect2.right || rect1.bottom < rect2.top || rect1.top > rect2.bottom);
    }
    function updateElementSelection() {
        const elements = container?.querySelectorAll('.selectable');
        elements?.forEach(element => {
            const elemRect = element.getBoundingClientRect();
            if (selectionRect && rectOverlap(selectionRect, elemRect)) {
                element.classList.contains('ring-2') ? isCtrlPressed && element.classList.remove('ring-2', 'ring-blue-500', 'shadow-lg') : element.classList.add('ring-2', 'ring-blue-500', 'shadow-lg');
            }
            else if (!isCtrlPressed) {
                element.classList.remove('ring-2', 'ring-blue-500', 'shadow-lg');
            }
        });
    }
    function onMouseMove(e) {
        if (!startPoint) {
            return;
        }
        selectionRect = createRectFromPoints(startPoint, {
            x: e.clientX,
            y: e.clientY
        });
        updateElementSelection();
    }
    function onTouchMove(e) {
        if (!startPoint || e.touches.length === 0) {
            return;
        }
        selectionRect = createRectFromPoints(startPoint, {
            x: e.touches[0].clientX,
            y: e.touches[0].clientY
        });
        updateElementSelection();
    }
    container.addEventListener('mousedown', (e) => {
        isCtrlPressed = e.ctrlKey;
        startPoint = {
            x: e.clientX,
            y: e.clientY
        };
        container.addEventListener('mousemove', onMouseMove);
    });
    container.addEventListener('mouseup', (e) => {
        if (startPoint) {
            container.removeEventListener('mousemove', onMouseMove);
            selectionRect = createRectFromPoints(startPoint, {
                x: e.clientX,
                y: e.clientY
            });
            updateElementSelection();
            dotNetHelper.invokeMethodAsync('UpdateSelectedElements', Array.from(container.querySelectorAll('.selectable.ring-2')).map(el => el.id));
            if (!isCtrlPressed) {
                startPoint = null;
                selectionRect = null;
            }
        }
    });
    container.addEventListener('touchstart', (e) => {
        isCtrlPressed = e.touches.length > 1;
        startPoint = {
            x: e.touches[0].clientX,
            y: e.touches[0].clientY
        };
        container.addEventListener('touchmove', onTouchMove);
    });
    container.addEventListener('touchend', (e) => {
        if (e.changedTouches && e.changedTouches.length > 0 && startPoint) {
            container.removeEventListener('touchmove', onTouchMove);
            selectionRect = createRectFromPoints(startPoint, {
                x: e.changedTouches[0].clientX,
                y: e.changedTouches[0].clientY
            });
            updateElementSelection();
            dotNetHelper.invokeMethodAsync('UpdateSelectedElements', Array.from(container.querySelectorAll('.selectable.ring-2')).map(el => el.id));
        }
        if (!isCtrlPressed) {
            startPoint = null;
            selectionRect = null;
        }
    });
}
//# sourceMappingURL=SelectableArea.js.map