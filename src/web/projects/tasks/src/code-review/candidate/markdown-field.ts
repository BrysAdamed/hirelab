import { DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmTextarea } from '@hirelab/ui/helm/textarea';
import { MarkdownViewer } from '@hirelab/ui/markdown';

type FieldView = 'write' | 'preview';

let nextId = 0;

/** A labelled Markdown textarea with a Write / Preview switch and a character counter. */
@Component({
  selector: 'hl-markdown-field',
  imports: [DecimalPipe, HlmButton, HlmTextarea, MarkdownViewer],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-2' },
  template: `
    <div class="flex flex-wrap items-center justify-between gap-2">
      <label class="font-medium" [for]="id">{{ label() }}</label>
      <div role="group" [attr.aria-label]="label() + ' view'" class="flex items-center gap-1">
        @for (option of views; track option.value) {
          <button
            hlmBtn
            type="button"
            size="sm"
            [variant]="view() === option.value ? 'secondary' : 'ghost'"
            [attr.aria-pressed]="view() === option.value"
            (click)="view.set(option.value)"
          >
            {{ option.label }}
          </button>
        }
      </div>
    </div>

    @if (view() === 'write') {
      <textarea
        #field
        hlmTextarea
        [class.min-h-48]="large()"
        [class.min-h-24]="!large()"
        [id]="id"
        [value]="value()"
        [attr.maxlength]="maxLength()"
        [aria-describedby]="id + '-hint'"
        (input)="valueChange.emit(field.value)"
      ></textarea>
    } @else if (value().trim()) {
      <hl-markdown
        class="rounded-md border p-3"
        [class.min-h-48]="large()"
        [class.min-h-24]="!large()"
        [markdown]="value()"
      />
    } @else {
      <p
        class="rounded-md border border-dashed p-3 text-muted-foreground"
        [class.min-h-48]="large()"
        [class.min-h-24]="!large()"
      >
        Nothing to preview yet.
      </p>
    }

    <p class="text-sm text-muted-foreground" [id]="id + '-hint'">
      You can use Markdown. {{ value().length | number }} of {{ maxLength() | number }} characters.
    </p>
  `,
})
export class MarkdownField {
  private readonly field = viewChild<ElementRef<HTMLTextAreaElement>>('field');

  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly maxLength = input.required<number>();
  readonly large = input(false);
  /** Focuses the textarea when the field first renders. */
  readonly focusOnInit = input(false);
  readonly valueChange = output<string>();

  protected readonly id = `hl-markdown-field-${nextId++}`;
  protected readonly views: readonly { value: FieldView; label: string }[] = [
    { value: 'write', label: 'Write' },
    { value: 'preview', label: 'Preview' },
  ];
  protected readonly view = signal<FieldView>('write');

  constructor() {
    afterNextRender(() => {
      if (this.focusOnInit()) {
        this.focus();
      }
    });
  }

  focus(): void {
    this.view.set('write');
    this.field()?.nativeElement.focus();
  }
}
