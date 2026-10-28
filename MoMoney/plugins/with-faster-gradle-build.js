const { withGradleProperties } = require('expo/config-plugins');

/** Speeds up release builds with a bigger Gradle heap. */
module.exports = (config) =>
  withGradleProperties(config, (cfg) => {
    const set = (key, value) => {
      cfg.modResults = cfg.modResults.filter((p) => !(p.type === 'property' && p.key === key));
      cfg.modResults.push({ type: 'property', key, value });
    };
    set('org.gradle.jvmargs', '-Xmx6g -XX:MaxMetaspaceSize=1g');
    return cfg;
  });
