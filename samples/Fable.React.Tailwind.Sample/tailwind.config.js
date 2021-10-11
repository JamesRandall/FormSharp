const defaultTheme = require('tailwindcss/defaultTheme')

module.exports = {
  theme: {
    /*backgroundColor: theme => ({
      ...theme('colors'),
      'primary': '#D64545',
      'secondary': '#F0B429',
      'primary-header-btn': '#FADB5F',
      'primary-header-btn-hover': '#FAF9F7'
    }),
    textColor: theme => ({
      ...theme('colors'),
      'primary': '#BA2525',
      'secondary': '#F0B429',
      'primary-header': '#27241d',
      'primary-header-btn': '#D64545'
    }),*/
    zIndex: {
      '0': 0,
      '10': 10,
      '20': 20,
      '30': 30,
      '40': 40,
      '50': 50,
      '25': 25,
      '50': 50,
      '75': 75,
      '100': 100,
      '800': 800,
      '1200': 1200,
      'auto': 'auto'
    },
    maxWidth: {
      '1/4': '25%',
      '1/2': '50%',
      '3/4': '75%',
     },
    extend: {
      fontFamily: {
        sans: ['Inter var', ...defaultTheme.fontFamily.sans]
      }
    },
  },
  variants: {
    backgroundColor: ['responsive', 'hover', 'focus', 'active', 'disabled'],
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/line-clamp'),
    require('tailwindcss'),
    //require('@tailwindcss/ui')
    require('autoprefixer')
  ],
}
