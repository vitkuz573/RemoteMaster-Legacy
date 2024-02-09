/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ["./Components/*.razor", "./Components/**/*.razor"],
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
