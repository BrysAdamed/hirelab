import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import type { TaskContentErrors } from '@hirelab/tasks/editor';
import { CodeEditor } from '@hirelab/ui/code';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import {
  type ChangedFile,
  type CodeLanguage,
  type DiffSide,
  codeLanguages,
  codeReviewLimits,
} from '../candidate/code-review';
import { FieldErrors, describedBy, messagesOf } from './field-errors';

type FileChange = 'modified' | 'added' | 'deleted';

const languageNames: Record<CodeLanguage, string> = {
  csharp: 'C#',
  typescript: 'TypeScript',
  javascript: 'JavaScript',
  html: 'HTML',
  css: 'CSS',
  json: 'JSON',
  sql: 'SQL',
  xml: 'XML',
  yaml: 'YAML',
  markdown: 'Markdown',
  plaintext: 'Plain text',
};

const fileChanges: readonly { value: FileChange; name: string }[] = [
  { value: 'modified', name: 'Modified' },
  { value: 'added', name: 'Added (no original)' },
  { value: 'deleted', name: 'Deleted (no modified)' },
];

let nextId = 0;

@Component({
  selector: 'hl-changed-file-editor',
  imports: [CodeEditor, FieldErrors, HlmButton, HlmInput, HlmLabel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <fieldset class="flex flex-col gap-4 rounded-md border p-4">
      <legend class="px-1 font-medium">Changed file {{ index() + 1 }}</legend>
      <div class="grid gap-4 md:grid-cols-[1fr_12rem_14rem]">
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-path'">Path</label>
          <input
            #pathInput
            hlmInput
            required
            [id]="id + '-path'"
            [value]="file().path"
            [attr.maxlength]="limits.pathMaxLength"
            [forceInvalid]="pathErrors().length > 0"
            [aria-describedby]="describedBy(id + '-path', pathErrors())"
            (input)="change({ path: pathInput.value })"
          />
          <hl-field-errors [fieldId]="id + '-path'" [messages]="pathErrors()" />
        </div>
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-language'">Language</label>
          <select
            #languageInput
            hlmInput
            [id]="id + '-language'"
            [forceInvalid]="languageErrors().length > 0"
            [aria-describedby]="describedBy(id + '-language', languageErrors())"
            (change)="change({ language: $any(languageInput.value) })"
          >
            @for (language of languages; track language) {
              <option [value]="language" [selected]="language === file().language">
                {{ languageNames[language] }}
              </option>
            }
          </select>
          <hl-field-errors [fieldId]="id + '-language'" [messages]="languageErrors()" />
        </div>
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-change'">Change</label>
          <select
            #changeInput
            hlmInput
            [id]="id + '-change'"
            (change)="changeKind($any(changeInput.value))"
          >
            @for (option of fileChanges; track option.value) {
              <option [value]="option.value" [selected]="option.value === kind()">
                {{ option.name }}
              </option>
            }
          </select>
        </div>
      </div>

      <div class="flex flex-col gap-2">
        <div
          class="flex flex-wrap items-center gap-2"
          role="group"
          [attr.aria-label]="'Side of changed file ' + (index() + 1)"
        >
          @for (side of sides(); track side) {
            <button
              hlmBtn
              type="button"
              size="sm"
              [variant]="side === shownSide() ? 'secondary' : 'ghost'"
              [attr.aria-pressed]="side === shownSide()"
              (click)="selectedSide.set(side)"
            >
              {{ side === 'original' ? 'Original' : 'Modified' }}
            </button>
          }
        </div>
        @if (file().original !== null && file().original !== undefined) {
          <hl-code-editor
            class="h-72"
            [hidden]="shownSide() !== 'original'"
            [value]="file().original ?? ''"
            [language]="file().language"
            [ariaLabel]="'Original text of changed file ' + (index() + 1)"
            (valueChange)="change({ original: $event })"
          />
        }
        @if (file().modified !== null && file().modified !== undefined) {
          <hl-code-editor
            class="h-72"
            [hidden]="shownSide() !== 'modified'"
            [value]="file().modified ?? ''"
            [language]="file().language"
            [ariaLabel]="'Modified text of changed file ' + (index() + 1)"
            (valueChange)="change({ modified: $event })"
          />
        }
        <hl-field-errors [fieldId]="id + '-text'" [messages]="textErrors()" />
      </div>

      <div>
        <button
          hlmBtn
          type="button"
          variant="outline"
          size="sm"
          [attr.aria-label]="'Remove changed file ' + (index() + 1)"
          (click)="remove.emit()"
        >
          Remove
        </button>
      </div>
    </fieldset>
  `,
})
export class ChangedFileEditor {
  readonly file = input.required<ChangedFile>();
  readonly index = input.required<number>();
  readonly errors = input<TaskContentErrors>({});
  readonly fileChange = output<ChangedFile>();
  readonly remove = output<void>();

  protected readonly id = `hl-changed-file-${nextId++}`;
  protected readonly limits = codeReviewLimits;
  protected readonly languages = codeLanguages;
  protected readonly languageNames = languageNames;
  protected readonly fileChanges = fileChanges;
  protected readonly describedBy = describedBy;
  protected readonly selectedSide = signal<DiffSide>('modified');

  private readonly field = computed(() => `candidateContent.files[${this.index()}]`);
  protected readonly pathErrors = computed(() => messagesOf(this.errors(), `${this.field()}.path`));
  protected readonly languageErrors = computed(() =>
    messagesOf(this.errors(), `${this.field()}.language`),
  );
  protected readonly textErrors = computed(() =>
    messagesOf(this.errors(), this.field(), `${this.field()}.original`, `${this.field()}.modified`),
  );

  protected readonly kind = computed<FileChange>(() => {
    const file = this.file();
    if (file.original === null || file.original === undefined) {
      return 'added';
    }
    return file.modified === null || file.modified === undefined ? 'deleted' : 'modified';
  });

  protected readonly sides = computed<readonly DiffSide[]>(() => {
    switch (this.kind()) {
      case 'added':
        return ['modified'];
      case 'deleted':
        return ['original'];
      default:
        return ['original', 'modified'];
    }
  });

  protected readonly shownSide = computed<DiffSide>(() => {
    const sides = this.sides();
    return sides.includes(this.selectedSide()) ? this.selectedSide() : sides[0];
  });

  protected change(change: Partial<ChangedFile>): void {
    this.fileChange.emit({ ...this.file(), ...change });
  }

  protected changeKind(kind: FileChange): void {
    const file = this.file();
    this.change({
      original: kind === 'added' ? null : (file.original ?? ''),
      modified: kind === 'deleted' ? null : (file.modified ?? ''),
    });
  }
}
