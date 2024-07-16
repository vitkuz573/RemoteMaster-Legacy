/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ["./Components/**/*.{razor,razor.cs}", "./wwwroot/js/*.js"],
    darkMode: "class",
    theme: {
        extend: {
            backgroundColor: {
                'de876c': "#DE876C"
            }
        }
    },
    variants: {
        extend: {}
    },
    plugins: []
}
