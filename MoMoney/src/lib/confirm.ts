import { Alert } from 'react-native';

/** Yes/No dialog as a promise (replaces Shell.DisplayAlertAsync with two buttons). */
export function confirm(title: string, message: string, yes = 'Yes', no = 'No'): Promise<boolean> {
  return new Promise((resolve) => {
    Alert.alert(
      title,
      message,
      [
        { text: no, style: 'cancel', onPress: () => resolve(false) },
        { text: yes, onPress: () => resolve(true) },
      ],
      { cancelable: true, onDismiss: () => resolve(false) },
    );
  });
}

/** Single-button dialog. */
export function notify(title: string, message: string): Promise<void> {
  return new Promise((resolve) => {
    Alert.alert(title, message, [{ text: 'OK', onPress: () => resolve() }], { cancelable: true, onDismiss: () => resolve() });
  });
}

/** Choose between two actions (e.g. "Necessary" / "Default"); resolves to the label or null. */
export function choose(title: string, message: string, options: string[]): Promise<string | null> {
  return new Promise((resolve) => {
    Alert.alert(
      title,
      message,
      [
        ...options.map((o) => ({ text: o, onPress: () => resolve(o) })),
        { text: 'Cancel', style: 'cancel' as const, onPress: () => resolve(null) },
      ],
      { cancelable: true, onDismiss: () => resolve(null) },
    );
  });
}
