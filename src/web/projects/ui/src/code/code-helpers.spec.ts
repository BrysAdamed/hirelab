import { deletedLineAt, mapOriginalLine, selectionLines, type LineChange } from './diff-lines';
import { parseKeybinding } from './keybinding';
import { buildMonacoTheme, THEME_TOKENS, type ThemeTokenColours } from './monaco-theme';
import { FakeMonaco } from './testing/fake-monaco';

describe('mapOriginalLine', () => {
  const changes: LineChange[] = [
    {
      originalStartLineNumber: 2,
      originalEndLineNumber: 0,
      modifiedStartLineNumber: 3,
      modifiedEndLineNumber: 4,
    },
    {
      originalStartLineNumber: 5,
      originalEndLineNumber: 6,
      modifiedStartLineNumber: 6,
      modifiedEndLineNumber: 0,
    },
    {
      originalStartLineNumber: 9,
      originalEndLineNumber: 9,
      modifiedStartLineNumber: 9,
      modifiedEndLineNumber: 11,
    },
  ];

  it.each([
    [1, 1, false],
    [2, 2, false],
    [3, 5, false],
    [5, 6, true],
    [6, 6, true],
    [7, 7, false],
    [9, 11, true],
    [10, 12, false],
  ])('maps original line %i to modified line %i (deleted: %s)', (line, expected, deleted) => {
    // Act
    const mapped = mapOriginalLine(line, changes);

    // Assert
    expect(mapped).toEqual({ line: expected, deleted });
  });

  it('finds the original line inside a deleted block', () => {
    // Act and assert
    expect(deletedLineAt(6, 0, changes)).toBe(5);
    expect(deletedLineAt(6, 1, changes)).toBe(6);
    expect(deletedLineAt(6, 5, changes)).toBe(6);
    expect(deletedLineAt(8, 0, changes)).toBe(9);
    expect(deletedLineAt(1, 0, changes)).toBeNull();
  });
});

describe('selectionLines', () => {
  it('drops a last line the selection only touches at column 1', () => {
    // Act and assert
    expect(selectionLines({ startLineNumber: 2, endLineNumber: 4, endColumn: 1 })).toEqual({
      startLine: 2,
      endLine: 3,
    });
    expect(selectionLines({ startLineNumber: 2, endLineNumber: 2, endColumn: 1 })).toEqual({
      startLine: 2,
      endLine: 2,
    });
  });
});

describe('parseKeybinding', () => {
  const monaco = new FakeMonaco();

  it('combines modifiers and the key', () => {
    // Act and assert
    expect(parseKeybinding('Ctrl+Alt+M', monaco)).toBe(2048 | 512 | 43);
    expect(parseKeybinding('Shift+F2', monaco)).toBe(1024 | 60);
    expect(parseKeybinding('Ctrl+1', monaco)).toBe(2048 | 22);
  });

  it('rejects unknown keys', () => {
    // Act and assert
    expect(() => parseKeybinding('Hyper+M', monaco)).toThrow(/modifier/);
    expect(() => parseKeybinding('Ctrl+Nope', monaco)).toThrow(/key/);
  });
});

describe('buildMonacoTheme', () => {
  const tokens = Object.fromEntries(
    THEME_TOKENS.map((token, index) => [token, `#${index.toString(16).padStart(6, '0')}`]),
  ) as ThemeTokenColours;

  it('maps the token sets onto the built-in themes', () => {
    // Act and assert
    expect(buildMonacoTheme('light', tokens).base).toBe('vs');
    expect(buildMonacoTheme('dark', tokens).base).toBe('vs-dark');
    expect(buildMonacoTheme('hc-light', tokens).base).toBe('hc-light');
    expect(buildMonacoTheme('hc-dark', tokens).base).toBe('hc-black');
  });

  it('colours the editor from the tokens', () => {
    // Act
    const theme = buildMonacoTheme('dark', tokens);

    // Assert
    expect(theme.colors['editor.background']).toBe(tokens.background);
    expect(theme.colors['diffEditor.insertedTextBackground']).toBe(`${tokens.success}40`);
    expect(theme.colors['contrastBorder']).toBeUndefined();
    expect(buildMonacoTheme('hc-dark', tokens).colors['contrastBorder']).toBe(tokens.border);
  });

  it('leaves out colours it could not read', () => {
    // Act
    const theme = buildMonacoTheme('light', { ...tokens, background: '' });

    // Assert
    expect(theme.colors['editor.background']).toBeUndefined();
  });
});
