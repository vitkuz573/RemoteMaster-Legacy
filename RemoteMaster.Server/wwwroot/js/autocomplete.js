function setupAutocomplete(inputId, suggestions) {
    const input = document.getElementById(inputId);
    input.addEventListener('input', function () {
        const val = this.value;
        let list = document.getElementById(inputId + "-autocomplete-list");
        if (list) {
            list.remove();
        }
        if (!val) {
            return false;
        }
        list = document.createElement('div');
        list.setAttribute('id', inputId + "-autocomplete-list");
        list.setAttribute('class', 'absolute z-10 bg-white border border-gray-300 mt-1 rounded shadow-lg');
        this.parentNode.appendChild(list);
        list.style.width = input.offsetWidth + "px";
        list.style.left = input.offsetLeft + "px";
        list.style.top = (input.offsetTop + input.offsetHeight) + "px";
        for (let i = 0; i < suggestions.length; i++) {
            if (suggestions[i].substr(0, val.length).toUpperCase() === val.toUpperCase()) {
                const item = document.createElement('div');
                item.classList.add('py-2', 'px-4', 'cursor-pointer', 'hover:bg-gray-200');
                item.innerHTML = "<strong>" + suggestions[i].substr(0, val.length) + "</strong>";
                item.innerHTML += suggestions[i].substr(val.length);
                item.innerHTML += "<input type='hidden' value='" + suggestions[i] + "'>";
                item.addEventListener('click', function (e) {
                    input.value = this.getElementsByTagName('input')[0].value;
                    closeAllLists();
                });
                list.appendChild(item);
            }
        }
    });

    function closeAllLists(elmnt) {
        const items = document.getElementsByClassName('autocomplete-items');
        for (let i = 0; i < items.length; i++) {
            if (elmnt !== items[i] && elmnt !== input) {
                items[i].parentNode.removeChild(items[i]);
            }
        }
    }

    document.addEventListener('click', function (e) {
        closeAllLists(e.target);
    });
}
