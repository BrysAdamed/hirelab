import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { HlmButton } from '@hirelab/ui/helm/button';
import { codeReviewLimits, type ReviewVerdict } from './code-review';
import { MarkdownField } from './markdown-field';
import { verdictLabels } from './verdict';

let nextId = 0;

/** The candidate's review summary (Markdown) and optional verdict. */
@Component({
  selector: 'hl-finish-review-panel',
  imports: [HlmButton, MarkdownField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'flex flex-col gap-4 rounded-md border p-6',
    role: 'region',
    '[attr.aria-labelledby]': 'id',
  },
  template: `
    <h3 class="text-lg font-semibold" [id]="id">Finish review</h3>
    <hl-markdown-field
      label="Review summary"
      [large]="true"
      [value]="summary()"
      [maxLength]="maxLength"
      (valueChange)="summaryChange.emit($event)"
    />
    <fieldset class="flex flex-col gap-2">
      <legend class="mb-2 font-medium">Verdict (optional)</legend>
      <div class="flex flex-wrap items-center gap-4">
        @for (option of verdicts; track option.value) {
          <label class="flex items-center gap-2">
            <input
              type="radio"
              class="size-4 accent-primary"
              [name]="id + '-verdict'"
              [value]="option.value"
              [checked]="verdict() === option.value"
              (change)="verdictChange.emit(option.value)"
            />
            {{ option.label }}
          </label>
        }
        @if (verdict()) {
          <button hlmBtn type="button" variant="ghost" size="sm" (click)="verdictChange.emit(null)">
            Clear verdict
          </button>
        }
      </div>
    </fieldset>
  `,
})
export class FinishReviewPanel {
  readonly summary = input.required<string>();
  readonly verdict = input<ReviewVerdict | null>(null);
  readonly summaryChange = output<string>();
  readonly verdictChange = output<ReviewVerdict | null>();

  protected readonly id = `hl-finish-review-${nextId++}`;
  protected readonly maxLength = codeReviewLimits.summaryMaxLength;
  protected readonly verdicts = (Object.keys(verdictLabels) as ReviewVerdict[]).map((value) => ({
    value,
    label: verdictLabels[value],
  }));
}
