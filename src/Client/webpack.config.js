// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

const path = require("path");
const webpack = require("webpack");
const HtmlWebpackPlugin = require('html-webpack-plugin');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CopyWebpackPlugin = require('copy-webpack-plugin');

var contentFolder = "./public";

var CONFIG = {
    devServerPort: 8080,
    // When using webpack-dev-server, you may need to redirect some calls
    // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
    devServerProxy: {
        '/api/*': {
            target: 'http://localhost:5000',
               changeOrigin: true
           },
       },
}

var commonPlugins = [
    new HtmlWebpackPlugin({
        filename: './index.html',
        template: './src/index.html'
    })
];

module.exports = (env, options) => {
    // If no mode has been defined, default to `development`
    if (options.mode === undefined)
        options.mode = "development";

    var isProduction = options.mode === "production";
    console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

    return {
            entry: isProduction ? // We don't use the same entry for dev and production, to make HMR over style quicker for dev env
                {
                    demo: [
                        './src/App.fsproj',
                        './src/main.scss'
                    ]
                } : {
                    app: [
                        './src/App.fsproj'
                    ],
                    style: [
                        './src/main.scss'
                    ]
                },
            plugins: isProduction ?
                commonPlugins.concat([
                    new MiniCssExtractPlugin({
                        filename: 'style.css'
                    }),
                    new CopyWebpackPlugin([
                        { from: './static' }
                    ])
                ])
                : commonPlugins.concat([
                    new webpack.HotModuleReplacementPlugin(),
                    new webpack.NamedModulesPlugin()
                ]),
            output: {
                path: path.join(__dirname, './output'),
                filename: isProduction ? '[name].[hash].js' : '[name].js'
            },
            devServer: {
                contentBase: contentFolder,
                port: CONFIG.devServerPort,
                proxy: CONFIG.devServerProxy,
            },
            module: {
                rules: [{
                    test: /\.fs(x|proj)?$/,
                    use: "fable-loader"
                },
                {
                    test: /\.(sass|scss|css)$/,
                    use: [
                        isProduction
                            ? MiniCssExtractPlugin.loader
                            : 'style-loader',
                        'css-loader',
                        'sass-loader',
                    ],
                },
                {
                    test: /\.css$/,
                    use: ['style-loader', 'css-loader']
                },
                {
                    test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*$|$)/,
                    use: ["file-loader"]
                }]
            }
    }
}
