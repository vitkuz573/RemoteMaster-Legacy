function preventEnterKeyWhenDrawerClosed(drawerOpen) {
    document.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && !drawerOpen) {
            event.preventDefault();
        }
    });
}