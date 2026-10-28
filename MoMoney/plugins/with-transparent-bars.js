const { withAndroidStyles } = require('expo/config-plugins');

/** Removes Android's forced contrast scrim so system bars stay fully transparent. */
module.exports = (config) =>
  withAndroidStyles(config, (cfg) => {
    const theme = cfg.modResults.resources.style?.find((s) => s.$.name === 'AppTheme');
    if (!theme) return cfg;
    const set = (name, value) => {
      theme.item = (theme.item ?? []).filter((i) => i.$.name !== name);
      theme.item.push({ $: { name }, _: value });
    };
    set('android:statusBarColor', '@android:color/transparent');
    set('android:navigationBarColor', '@android:color/transparent');
    set('android:enforceNavigationBarContrast', 'false');
    set('android:enforceStatusBarContrast', 'false');
    return cfg;
  });
