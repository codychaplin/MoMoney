const { withGradleProperties } = require('expo/config-plugins');

/** Speeds up release builds: ARM-only native code (no emulator ABIs) and a bigger Gradle heap. */
module.exports = (config) =>
  withGradleProperties(config, (cfg) => {
    const set = (key, value) => {
      cfg.modResults = cfg.modResults.filter((p) => !(p.type === 'property' && p.key === key));
      cfg.modResults.push({ type: 'property', key, value });
    };
    set('reactNativeArchitectures', 'armeabi-v7a,arm64-v8a');
    set('org.gradle.jvmargs', '-Xmx6g -XX:MaxMetaspaceSize=1g');
    return cfg;
  });
