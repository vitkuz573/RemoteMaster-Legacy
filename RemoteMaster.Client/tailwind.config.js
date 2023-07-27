module.exports = {
    content: ['./**/*.html', './**/*.razor'],
    darkMode: 'media', // or 'class'
    theme: {
        extend: {},
    },
    variants: {
        extend: {},
    },
    plugins: [
        require('@tailwindcss/forms')({
            strategy: 'class',
            reset: {
                'button[type="submit"]': false,
                button: false,
            },
        }),
    ],
}
