// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");

var contentFolder = "./public";

var CONFIG = {
    devServerPort: 8080,
    // When using webpack-dev-server, you may need to redirect some calls
    // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
    devServerProxy: {
        '/SignalR/*': {
            target: 'http://localhost:5000',
               changeOrigin: true
           },
           '/api/*': {
            target: 'http://localhost:5000',
               changeOrigin: true
           },
       },
}

module.exports = {
    mode: "development",
    entry: "./src/App.fsproj",
    output: {
        path: path.join(__dirname, contentFolder),
        filename: "bundle.js",
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
        }]
    }
}
