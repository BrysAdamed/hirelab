import type { editor } from 'monaco-editor/editor/editor.api.js';

/** Options every HireLab Monaco editor shares. */
export function baseEditorOptions(fontFamily: string): editor.IEditorOptions {
  return {
    automaticLayout: true,
    fixedOverflowWidgets: true,
    minimap: { enabled: false },
    scrollBeyondLastLine: false,
    fontFamily,
    fontSize: 13,
    lineHeight: 20,
  };
}

export function setModelValue(model: editor.ITextModel, value: string): void {
  if (model.getValue() !== value) {
    model.setValue(value);
  }
}
