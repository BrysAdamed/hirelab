export interface KeyCodes {
  readonly KeyMod: { readonly CtrlCmd: number; readonly Alt: number; readonly Shift: number };
  readonly KeyCode: object;
}

/** Parses a shortcut such as `Ctrl+Alt+M` or `Shift+F2` into a Monaco keybinding. `Ctrl` is `Cmd` on macOS. */
export function parseKeybinding(keys: string, monaco: KeyCodes): number {
  const parts = keys.split('+').map((part) => part.trim());
  const key = parts.pop() ?? '';
  let binding = 0;
  for (const modifier of parts.map((part) => part.toLowerCase())) {
    if (modifier === 'ctrl' || modifier === 'cmd') {
      binding |= monaco.KeyMod.CtrlCmd;
    } else if (modifier === 'alt') {
      binding |= monaco.KeyMod.Alt;
    } else if (modifier === 'shift') {
      binding |= monaco.KeyMod.Shift;
    } else {
      throw new Error(`Unknown modifier '${modifier}' in '${keys}'.`);
    }
  }
  const name = /^[a-z]$/i.test(key)
    ? `Key${key.toUpperCase()}`
    : /^\d$/.test(key)
      ? `Digit${key}`
      : key;
  const code = (monaco.KeyCode as unknown as Record<string, number | undefined>)[name];
  if (code === undefined) {
    throw new Error(`Unknown key '${key}' in '${keys}'.`);
  }
  return binding | code;
}
