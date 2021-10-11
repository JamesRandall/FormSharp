// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var HtmlWebpackPlugin = require('html-webpack-plugin');

var path = require("path");
var isProduction = !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1);

function resolve(filePath) {
  return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}


module.exports = {
    mode: "development",
    entry: "./build/App.js",
    output: {
        path: path.join(__dirname, "./public"),
        filename: "bundle.js",
    },
    devServer: {
        publicPath: "/",
        contentBase: "./public",
        port: 8080,
    },
    plugins: [
      new HtmlWebpackPlugin({
        filename: 'index.html',
        template: resolve('./src/index.html')
      })
    ],
    module: {
      rules: [
        {
          test: /\.(sass|scss|css)$/,
          use: [
              isProduction
                  ? MiniCssExtractPlugin.loader
                  : 'style-loader',
              'css-loader',
              'postcss-loader'
          ]
        },
        {
          test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
          use: ['file-loader']
        }
      ]
    }
  }
