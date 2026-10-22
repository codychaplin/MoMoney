import { Alert, type AlertButton } from 'react-native';

/**
 * Auto-answers Alert.alert. 'yes' presses the last button (OK / Yes), 'no' the first (No / Cancel),
 * or pass a button label. Returns the spy so tests can assert on titles and messages.
 */
export function mockAlert(answer: 'yes' | 'no' | string = 'yes') {
  return jest.spyOn(Alert, 'alert').mockImplementation((_title, _msg, buttons?: AlertButton[]) => {
    if (!buttons?.length) return;
    const button = answer === 'yes' ? buttons.at(-1) : answer === 'no' ? buttons[0] : buttons.find((b) => b.text === answer);
    button?.onPress?.();
  });
}

/** [title, message] of every alert shown so far. */
export const shownAlerts = (spy: jest.SpyInstance) => spy.mock.calls.map((c) => [c[0], c[1]]);

/** Answer each alert by title: a button label, or 'yes' / 'no'. Alerts not listed are answered with `fallback`. */
export function mockAlertByTitle(answers: Record<string, string>, fallback = 'yes') {
  return jest.spyOn(Alert, 'alert').mockImplementation((title, _msg, buttons?: AlertButton[]) => {
    const answer = answers[String(title)] ?? fallback;
    const button = answer === 'yes' ? buttons?.at(-1) : answer === 'no' ? buttons?.[0] : buttons?.find((b) => b.text === answer);
    button?.onPress?.();
  });
}
