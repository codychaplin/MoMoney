import type { ReactNode } from 'react';
import { View } from 'react-native';

/** Controllable stand-in for expo-router: tests assert on `mockRouter` and set params with `setParams`. */
export const mockRouter = { push: jest.fn(), back: jest.fn(), replace: jest.fn(), navigate: jest.fn() };
let mockParams: Record<string, string> = {};
let mockSegments: string[] = [];

export const setParams = (p: Record<string, string>) => {
  mockParams = p;
};
export const setSegments = (s: string[]) => {
  mockSegments = s;
};
export const resetRouter = () => {
  Object.values(mockRouter).forEach((f) => f.mockReset());
  mockParams = {};
  mockSegments = [];
};

const Container = ({ children }: { children?: ReactNode }) => <View>{children}</View>;
/** Renders the tab options callbacks so layouts' icon/button render functions run. */
const Screen = ({ options }: { options?: any }) => (
  <View>
    {options?.tabBarButton?.({})}
    {options?.tabBarIcon?.({ color: '#000', size: 24, focused: true })}
    {options?.tabBarIcon?.({ color: '#000', size: 24, focused: false })}
  </View>
);

export const expoRouterMock = {
  useRouter: () => mockRouter,
  useLocalSearchParams: () => mockParams,
  useSegments: () => mockSegments,
  Stack: Object.assign(Container, { Screen }),
  Tabs: Object.assign(Container, { Screen }),
  Link: Container,
};
