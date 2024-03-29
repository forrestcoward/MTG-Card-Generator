const path = require('path')
const CopyWebpackPlugin = require('copy-webpack-plugin');
module.exports = {
  entry: './src/Index.tsx',
  output: {
    path: path.resolve(__dirname, './dist'),
    filename: 'bundle.js',
    assetModuleFilename: 'images/[hash][ext][query]',
    publicPath: '/', // Required for react router.
  },
  devServer: {
    hot: true,
    historyApiFallback: true, // Required for react router.
    client: {
      progress: true,
    },
  },
  plugins: [
    new CopyWebpackPlugin({
      patterns: [
        { from: './openapi.yaml', to: '.well-known/openapi.yaml' },
        { from: './privacy-policy.txt', to: '.well-known/privacy-policy.txt'}
      ]
    })
  ],
  module: {
    rules: [
      {
        test: /\.(js|ts)x?$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader',
        },
      },
      {
        test: /\.css$/i,
        exclude: /node_modules/,
        use: ["style-loader", "css-loader"],
      },
      {
        test: /\.png/,
        type: 'asset/resource'
      },
      {
        test: /\.jpg/,
        type: 'asset/resource'
      }
    ],
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.jsx', '.js', '.css'],
  }
}