import { logEvent } from '@react-native-firebase/analytics';

/** Names of the analytics events sent so far in the current test, in order. */
export const sentEvents = (): string[] => (logEvent as jest.Mock).mock.calls.map((c) => c[1]);

/** Params of the most recent send of `name`. */
export const lastParams = (name: string) => {
  const call = [...(logEvent as jest.Mock).mock.calls].reverse().find((c) => c[1] === name);
  return call?.[2];
};
