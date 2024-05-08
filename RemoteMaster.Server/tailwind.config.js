/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ["./Components/*.razor", "./Components/*.razor.cs", "./Components/**/*.razor", "./Components/**/*.razor.cs"],
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
