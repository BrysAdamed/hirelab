/** The languages a code block can be shown in; each maps to a Monaco grammar (ADR 0032). */
export type CodeLanguage =
  | 'csharp'
  | 'typescript'
  | 'javascript'
  | 'html'
  | 'css'
  | 'json'
  | 'sql'
  | 'xml'
  | 'yaml'
  | 'markdown'
  | 'plaintext';

export const CODE_LANGUAGES: readonly CodeLanguage[] = [
  'csharp',
  'typescript',
  'javascript',
  'html',
  'css',
  'json',
  'sql',
  'xml',
  'yaml',
  'markdown',
  'plaintext',
];

/** One side of a diff: the file before (`original`) or after (`modified`) the change. */
export type DiffSide = 'original' | 'modified';

/** A line on one side of a diff. Lines are 1-based. */
export interface DiffLine {
  readonly side: DiffSide;
  readonly line: number;
}

/** An inclusive range of lines on one side of a diff. Lines are 1-based. */
export interface DiffLineRange {
  readonly side: DiffSide;
  readonly startLine: number;
  readonly endLine: number;
}

/** How a decorated line range looks: a review comment's lines or an expected finding's lines. */
export type CodeDecorationKind = 'comment' | 'finding';

/** Highlights a line range in the diff, with an optional hover text (Markdown). */
export interface CodeLineDecoration extends DiffLineRange {
  readonly kind: CodeDecorationKind;
  readonly hoverMessage?: string;
}

/** Content rendered under a line of the diff (for example the review comments on it). */
export interface CodeViewZone<T = unknown> {
  readonly id: string;
  readonly side: DiffSide;
  readonly afterLine: number;
  readonly data: T;
}

/** A keyboard shortcut the diff offers, such as `Ctrl+Alt+M`. `Ctrl` is `Cmd` on macOS. */
export interface CodeKeyAction {
  readonly id: string;
  readonly label: string;
  readonly keys: string;
}

/** Emitted when a key action runs: which action and the current line range. */
export interface CodeKeyActionEvent {
  readonly id: string;
  readonly range: DiffLineRange;
}
