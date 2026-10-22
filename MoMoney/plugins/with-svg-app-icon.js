const { withDangerousMod } = require('expo/config-plugins');
const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');

/**
 * Generates the Android launcher icon from icon/icon_foreground.svg and icon/icon_background.svg using react-native-svg-app-icon
 * (vector adaptive icon + PNG fallbacks) and replaces the raster icons Expo prebuild writes.
 */
module.exports = (config) =>
  withDangerousMod(config, [
    'android',
    (cfg) => {
      const root = cfg.modRequest.projectRoot;
      const res = path.join(cfg.modRequest.platformProjectRoot, 'app/src/main/res');
      const foreground = path.join(root, 'icon/icon_foreground.svg');
      const background = path.join(root, 'icon/icon_background.svg');
      for (const file of [foreground, background]) {
        if (!fs.existsSync(file)) throw new Error(`with-svg-app-icon: missing ${file}`);
      }

      // Expo's webp launcher icons would clash with the generated PNGs of the same name.
      for (const dir of fs.readdirSync(res)) {
        if (!dir.startsWith('mipmap-')) continue;
        for (const file of fs.readdirSync(path.join(res, dir))) {
          if (/^ic_launcher.*\.(webp|png)$/.test(file)) fs.rmSync(path.join(res, dir, file));
        }
      }

      execFileSync(
        process.execPath,
        [
          require.resolve('react-native-svg-app-icon/dist/cli/index.js'),
          '--platforms', 'android',
          '--foreground-path', foreground,
          '--background-path', background,
          '--android-output-path', res,
          '--force',
          '--log-level', 'warn',
        ],
        { stdio: 'inherit' }
      );

      // Themed (Material You) icon: reuse the foreground vector as the monochrome layer.
      for (const name of ['ic_launcher.xml', 'ic_launcher_round.xml']) {
        const file = path.join(res, 'mipmap-anydpi-v26', name);
        const xml = fs.readFileSync(file, 'utf8');
        fs.writeFileSync(
          file,
          xml.replace(
            '</adaptive-icon>',
            '    <monochrome android:drawable="@drawable/ic_launcher_foreground" />\n</adaptive-icon>'
          )
        );
      }
      return cfg;
    },
  ]);
