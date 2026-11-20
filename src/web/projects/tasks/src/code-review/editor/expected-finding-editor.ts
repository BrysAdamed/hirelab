import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import type { TaskContentErrors } from '@hirelab/tasks/editor';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { HlmTextarea } from '@hirelab/ui/helm/textarea';
import {
  type DiffSide,
  type ExpectedFinding,
  codeReviewLimits,
  findingSeverities,
} from '../candidate/code-review';
import { FieldErrors, describedBy, messagesOf } from './field-errors';

const sides: readonly { value: DiffSide; name: string }[] = [
  { value: 'original', name: 'Original' },
  { value: 'modified', name: 'Modified' },
];

let nextId = 0;

@Component({
  selector: 'hl-expected-finding-editor',
  imports: [FieldErrors, HlmButton, HlmInput, HlmLabel, HlmTextarea],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <fieldset class="flex flex-col gap-4 rounded-md border p-4">
      <legend class="px-1 font-medium">Expected finding {{ index() + 1 }}</legend>
      <hl-field-errors [fieldId]="id" [messages]="findingErrors()" />
      <div class="grid gap-4 md:grid-cols-[1fr_14rem]">
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-title'">Title</label>
          <input
            #titleInput
            hlmInput
            required
            [id]="id + '-title'"
            [value]="finding().title"
            [attr.maxlength]="limits.titleMaxLength"
            [forceInvalid]="fieldErrors().title.length > 0"
            [aria-describedby]="describedBy(id + '-title', fieldErrors().title)"
            (input)="change({ title: titleInput.value })"
          />
          <hl-field-errors [fieldId]="id + '-title'" [messages]="fieldErrors().title" />
        </div>
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-id'">Id</label>
          <input
            #idInput
            hlmInput
            required
            [id]="id + '-id'"
            [value]="finding().id"
            [forceInvalid]="fieldErrors().id.length > 0"
            [aria-describedby]="describedBy(id + '-id', fieldErrors().id, true)"
            (input)="change({ id: idInput.value })"
          />
          <p class="text-muted-foreground text-sm" [id]="id + '-id-hint'">
            Kebab-case and unique in the task, for example missing-null-check.
          </p>
          <hl-field-errors [fieldId]="id + '-id'" [messages]="fieldErrors().id" />
        </div>
      </div>

      <div class="grid gap-4 md:grid-cols-[1fr_10rem_8rem_8rem_10rem]">
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-file'">File</label>
          <select
            #fileInput
            hlmInput
            required
            [id]="id + '-file'"
            [forceInvalid]="fieldErrors().file.length > 0"
            [aria-describedby]="describedBy(id + '-file', fieldErrors().file)"
            (change)="change({ file: fileInput.value })"
          >
            @if (!finding().file) {
              <option value="" disabled selected>Choose a file</option>
            }
            @for (path of fileOptions(); track path) {
              <option [value]="path" [selected]="path === finding().file">{{ path }}</option>
            }
          </select>
          <hl-field-errors [fieldId]="id + '-file'" [messages]="fieldErrors().file" />
        </div>
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-side'">Side</label>
          <select
            #sideInput
            hlmInput
            [id]="id + '-side'"
            [forceInvalid]="fieldErrors().side.length > 0"
            [aria-describedby]="describedBy(id + '-side', fieldErrors().side)"
            (change)="change({ side: $any(sideInput.value) })"
          >
            @for (side of sides; track side.value) {
              <option [value]="side.value" [selected]="side.value === finding().side">
                {{ side.name }}
              </option>
            }
          </select>
          <hl-field-errors [fieldId]="id + '-side'" [messages]="fieldErrors().side" />
        </div>
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-start-line'">Start line</label>
          <input
            #startLineInput
            hlmInput
            type="number"
            min="1"
            step="1"
            required
            [id]="id + '-start-line'"
            [value]="finding().startLine"
            [forceInvalid]="fieldErrors().startLine.length > 0"
            [aria-describedby]="describedBy(id + '-start-line', fieldErrors().startLine)"
            (change)="change({ startLine: lineOf(startLineInput) })"
          />
          <hl-field-errors [fieldId]="id + '-start-line'" [messages]="fieldErrors().startLine" />
        </div>
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-end-line'">End line</label>
          <input
            #endLineInput
            hlmInput
            type="number"
            min="1"
            step="1"
            required
            [id]="id + '-end-line'"
            [value]="finding().endLine"
            [forceInvalid]="fieldErrors().endLine.length > 0"
            [aria-describedby]="describedBy(id + '-end-line', fieldErrors().endLine)"
            (change)="change({ endLine: lineOf(endLineInput) })"
          />
          <hl-field-errors [fieldId]="id + '-end-line'" [messages]="fieldErrors().endLine" />
        </div>
        <div class="flex flex-col gap-1.5">
          <label hlmLabel [for]="id + '-severity'">Severity</label>
          <select
            #severityInput
            hlmInput
            [id]="id + '-severity'"
            [forceInvalid]="fieldErrors().severity.length > 0"
            [aria-describedby]="describedBy(id + '-severity', fieldErrors().severity)"
            (change)="change({ severity: $any(severityInput.value) })"
          >
            @for (severity of severities; track severity) {
              <option [value]="severity" [selected]="severity === finding().severity">
                {{ severity }}
              </option>
            }
          </select>
          <hl-field-errors [fieldId]="id + '-severity'" [messages]="fieldErrors().severity" />
        </div>
      </div>

      <div class="flex flex-col gap-1.5">
        <label hlmLabel [for]="id + '-description'">Description</label>
        <textarea
          #descriptionInput
          hlmTextarea
          class="min-h-24 font-mono"
          [id]="id + '-description'"
          [value]="finding().description"
          [attr.maxlength]="limits.descriptionMaxLength"
          [forceInvalid]="fieldErrors().description.length > 0"
          [aria-describedby]="describedBy(id + '-description', fieldErrors().description, true)"
          (input)="change({ description: descriptionInput.value })"
        ></textarea>
        <p class="text-muted-foreground text-sm" [id]="id + '-description-hint'">
          What is wrong and why, in Markdown.
        </p>
        <hl-field-errors [fieldId]="id + '-description'" [messages]="fieldErrors().description" />
      </div>

      <div>
        <button
          hlmBtn
          type="button"
          variant="outline"
          size="sm"
          [attr.aria-label]="'Remove expected finding ' + (index() + 1)"
          (click)="remove.emit()"
        >
          Remove
        </button>
      </div>
    </fieldset>
  `,
})
export class ExpectedFindingEditor {
  readonly finding = input.required<ExpectedFinding>();
  readonly index = input.required<number>();
  readonly paths = input.required<readonly string[]>();
  readonly errors = input<TaskContentErrors>({});
  readonly findingChange = output<ExpectedFinding>();
  readonly remove = output<void>();

  protected readonly id = `hl-expected-finding-${nextId++}`;
  protected readonly limits = codeReviewLimits;
  protected readonly sides = sides;
  protected readonly severities = findingSeverities;
  protected readonly describedBy = describedBy;

  protected readonly fileOptions = computed(() => {
    const file = this.finding().file;
    const paths = this.paths();
    return file && !paths.includes(file) ? [...paths, file] : paths;
  });

  private readonly field = computed(() => `staffContent.expectedFindings[${this.index()}]`);
  protected readonly findingErrors = computed(() => messagesOf(this.errors(), this.field()));
  protected readonly fieldErrors = computed(() => {
    const errors = this.errors();
    const field = this.field();
    const of = (name: string) => messagesOf(errors, `${field}.${name}`);
    return {
      id: of('id'),
      file: of('file'),
      side: of('side'),
      startLine: of('startLine'),
      endLine: of('endLine'),
      title: of('title'),
      description: of('description'),
      severity: of('severity'),
    };
  });

  protected change(change: Partial<ExpectedFinding>): void {
    this.findingChange.emit({ ...this.finding(), ...change });
  }

  protected lineOf(element: HTMLInputElement): number {
    return Math.trunc(element.valueAsNumber) || 0;
  }
}
