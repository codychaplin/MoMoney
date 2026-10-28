import { fireEvent, screen } from '@testing-library/react-native';

/** Open the dropdown labelled `label` (the `nth` one when a screen has several) and pick `option`. */
export async function choose(label: string, option: string, nth = 0) {
  await fireEvent.press(screen.getAllByLabelText(label)[nth]);
  // options can load asynchronously (e.g. rules for the selected bank)
  await fireEvent.press(await screen.findByRole('menuitem', { name: option }));
}

export const type = (label: string, text: string, nth = 0) => fireEvent.changeText(screen.getAllByLabelText(label)[nth], text);
export const tap = (text: string) => fireEvent.press(screen.getByText(text));
