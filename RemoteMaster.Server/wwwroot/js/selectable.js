
let isDragging = false;
let isCtrlPressed = false;
let isShiftPressed = false;
let startSelectionPosition = null;
let lastClickedIndex = null;
const selectionBox = document.createElement('div');
selectionBox.className = 'selection-box';
document.querySelector('.selectable-container').appendChild(selectionBox);

const items = Array.from(document.querySelectorAll('.selectable-item'));
const container = document.querySelector('.selectable-container');

container.addEventListener('mousedown', function(e) {
    if (e.button === 0) {
        if (e.target === container) {
            if (!isCtrlPressed && !isShiftPressed) {
                deselectAll();
            }
            isDragging = true;
            startSelectionPosition = { x: e.clientX + container.scrollLeft - container.getBoundingClientRect().left, y: e.clientY + container.scrollTop - container.getBoundingClientRect().top };
            selectionBox.style.left = `${startSelectionPosition.x}px`;
            selectionBox.style.top = `${startSelectionPosition.y}px`;
        } else if (e.target.classList.contains('selectable-item')) {
            if (isShiftPressed && lastClickedIndex !== null) {
                const currentIndex = items.indexOf(e.target);
                const startIndex = Math.min(currentIndex, lastClickedIndex);
                const endIndex = Math.max(currentIndex, lastClickedIndex);
                for (let i = startIndex; i <= endIndex; i++) {
                    items[i].classList.add('selected');
                }
            } else if (!isCtrlPressed) {
                deselectAll();
                e.target.classList.add('selected');
                lastClickedIndex = items.indexOf(e.target);
            } else {
                e.target.classList.toggle('selected');
                lastClickedIndex = items.indexOf(e.target);
            }
        }
    }
});

container.addEventListener('mousemove', function(e) {
    if (isDragging) {
        const x = Math.min(e.clientX + container.scrollLeft, startSelectionPosition.x);
        const y = Math.min(e.clientY + container.scrollTop, startSelectionPosition.y);
        const width = Math.abs(e.clientX - startSelectionPosition.x) - container.scrollLeft;
        const height = Math.abs(e.clientY - startSelectionPosition.y) - container.scrollTop;

        selectionBox.style.left = `${x}px`;
        selectionBox.style.top = `${y}px`;
        selectionBox.style.width = `${width}px`;
        selectionBox.style.height = `${height}px`;
        selectionBox.style.display = 'block';

        items.forEach(item => {
            const itemPosition = item.getBoundingClientRect();
            items.forEach(item => {
    const itemPosition = item.getBoundingClientRect();
    const itemLeft = itemPosition.left + container.scrollLeft;
    const itemRight = itemPosition.right + container.scrollLeft;
    const itemTop = itemPosition.top + container.scrollTop;
    const itemBottom = itemPosition.bottom + container.scrollTop;

    // Check if any part of the item intersects with the selection box
    const intersects = (
        itemLeft < x + width &&
        itemRight > x &&
        itemTop < y + height &&
        itemBottom > y
    );

    if (intersects) {
        item.classList.add('selected');
    } else if (!isCtrlPressed && !isShiftPressed) {
        item.classList.remove('selected');
    }
});
        });
    }
});

document.addEventListener('mouseup', function(e) {
    isDragging = false;
    selectionBox.style.display = 'none';
});

document.addEventListener('keydown', function(e) {
    if (e.key === 'Control') {
        isCtrlPressed = true;
    }
    if (e.key === 'Shift') {
        isShiftPressed = true;
    }
});

document.addEventListener('keyup', function(e) {
    if (e.key === 'Control') {
        isCtrlPressed = false;
    }
    if (e.key === 'Shift') {
        isShiftPressed = false;
    }
});

function deselectAll() {
    items.forEach(item => item.classList.remove('selected'));
}

window.getSelectedItems = function() {
    const selected = document.querySelectorAll('.selectable-item.selected');
    return Array.from(selected);
}

window.setContextMenu = function(menuElement) {
    container.addEventListener('contextmenu', function(e) {
        e.preventDefault();
        if (e.target.classList.contains('selected') || e.target.parentElement.classList.contains('selected')) {
            menuElement.style.left = e.clientX + 'px';
            menuElement.style.top = e.clientY + 'px';
            menuElement.style.display = 'block';
        }
    });

    document.addEventListener('click', function(e) {
        if (e.button === 0) {
            menuElement.style.display = 'none';
        }
    });
}

// Initializing the context menu on page load
document.addEventListener('DOMContentLoaded', function() {
    const menuElement = document.getElementById('sampleContextMenu');
    setContextMenu(menuElement);
});
