import 'monaco-editor/features/register.all.js';
import 'monaco-editor/editor/browser/coreCommands.js';
import 'monaco-editor/editor/contrib/caretOperations/browser/caretOperations.js';
import 'monaco-editor/editor/contrib/dropOrPasteInto/browser/copyPasteContribution.js';
import 'monaco-editor/editor/contrib/find/browser/findController.js';
import 'monaco-editor/editor/contrib/gotoSymbol/browser/goToCommands.js';
import 'monaco-editor/editor/contrib/gotoError/browser/markerSelectionStatus.js';
import 'monaco-editor/editor/contrib/semanticTokens/browser/documentSemanticTokens.js';
import 'monaco-editor/editor/contrib/suggest/browser/suggestController.js';
import 'monaco-editor/editor/common/standaloneStrings.js';

import 'monaco-editor/languages/definitions/csharp/register.js';
import 'monaco-editor/languages/definitions/css/register.js';
import 'monaco-editor/languages/definitions/html/register.js';
import 'monaco-editor/languages/definitions/javascript/register.js';
import 'monaco-editor/languages/definitions/markdown/register.js';
import 'monaco-editor/languages/definitions/sql/register.js';
import 'monaco-editor/languages/definitions/typescript/register.js';
import 'monaco-editor/languages/definitions/xml/register.js';
import 'monaco-editor/languages/definitions/yaml/register.js';
import { jsonDefaults } from 'monaco-editor/languages/features/json/register.js';

import * as monaco from 'monaco-editor/editor/editor.api.js';

self.MonacoEnvironment = {
  getWorker: (_workerId: string, label: string) =>
    label === 'json'
      ? new Worker(new URL('./json.worker', import.meta.url), { type: 'module' })
      : new Worker(new URL('./editor.worker', import.meta.url), { type: 'module' }),
};

jsonDefaults.setDiagnosticsOptions({ validate: false });

export { monaco };
