// EAS supplies google-services.json as a file env var (GOOGLE_SERVICES_JSON); locally it sits next to app.json (gitignored).
module.exports = ({ config }) => ({
  ...config,
  android: {
    ...config.android,
    googleServicesFile: process.env.GOOGLE_SERVICES_JSON ?? './google-services.json',
  },
});
