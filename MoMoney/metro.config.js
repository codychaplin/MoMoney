const { getDefaultConfig } = require('expo/metro-config');
const { withUniwindConfig } = require('uniwind/metro');

const config = getDefaultConfig(__dirname);

// Metro bundles every .env* file in the root in dev; keep the signing secrets (not a real env file) out.
config.resolver.blockList = [].concat(config.resolver.blockList ?? [], /\.env\.signing\.local$/);

module.exports = withUniwindConfig(config, {
  cssEntryFile: './src/global.css',
  dtsFile: './src/uniwind-types.d.ts',
});
