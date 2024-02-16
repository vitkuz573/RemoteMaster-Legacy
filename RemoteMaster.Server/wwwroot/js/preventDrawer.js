function preventDefaultForKeydownWhenDrawerClosed(drawerOpen) {
    document.addEventListener("keydown", function (event) {
        if ((event.key === "Enter" || event.key === " " || event.key === "Spacebar") && !drawerOpen) {
            event.preventDefault();
        }
    });
}