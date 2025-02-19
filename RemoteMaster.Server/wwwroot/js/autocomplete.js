export function setupAutocomplete(inputId, suggestions) {
    const input = document.getElementById(inputId);
    input.addEventListener('input', function () {
        const val = this.value;
        let list = document.getElementById(inputId + "-autocomplete-list");
        if (list) {
            list.remove();
        }
        if (!val) {
            return;
        }
        list = document.createElement('div');
        list.setAttribute('id', inputId + "-autocomplete-list");
        list.setAttribute('class', 'absolute z-10 bg-white border border-gray-300 mt-1 rounded-sm shadow-lg');
        this.parentNode?.appendChild(list);
        list.style.width = `${input.offsetWidth}px`;
        list.style.left = `${input.offsetLeft}px`;
        list.style.top = `${input.offsetTop + input.offsetHeight}px`;
        suggestions.forEach(suggestion => {
            if (suggestion.substr(0, val.length).toUpperCase() === val.toUpperCase()) {
                const item = document.createElement('div');
                item.classList.add('py-2', 'px-4', 'cursor-pointer', 'hover:bg-gray-200');
                item.innerHTML = `<strong>${suggestion.substr(0, val.length)}</strong>${suggestion.substr(val.length)}`;
                item.innerHTML += `<input type='hidden' value='${suggestion}'>`;
                item.addEventListener('click', function () {
                    input.value = this.getElementsByTagName('input')[0].value;
                    closeAllLists();
                });
                list.appendChild(item);
            }
        });
    });
    function closeAllLists(elmnt) {
        const items = document.querySelectorAll('.autocomplete-items');
        items.forEach(item => {
            if (elmnt !== item && elmnt !== input) {
                item.parentNode?.removeChild(item);
            }
        });
    }
    document.addEventListener('click', function (e) {
        closeAllLists(e.target);
    });
}
//# sourceMappingURL=autocomplete.js.map