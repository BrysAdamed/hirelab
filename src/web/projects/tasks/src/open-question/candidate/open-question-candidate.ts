import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmTextarea } from '@hirelab/ui/helm/textarea';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import type { CandidateTaskComponent } from '@hirelab/tasks/candidate';
import {
  answerMaxLength,
  type OpenQuestionContent,
  type OpenQuestionResponse,
} from './open-question';

type AnswerView = 'write' | 'preview';

let nextId = 0;

@Component({
  selector: 'hl-open-question-candidate',
  imports: [DecimalPipe, HlmButton, HlmTextarea, MarkdownViewer],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-6' },
  template: `
    <hl-markdown [markdown]="content().prompt" />

    <div class="flex flex-col gap-2">
      <div class="flex flex-wrap items-center justify-between gap-2">
        <label class="font-medium" [for]="id">Your answer</label>
        <div role="group" aria-label="Answer view" class="flex items-center gap-1">
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
          #answerInput
          hlmTextarea
          class="min-h-64"
          [id]="id"
          [value]="answer()"
          [attr.maxlength]="maxLength"
          [aria-describedby]="id + '-hint'"
          (input)="responseChange.emit({ answer: answerInput.value })"
        ></textarea>
      } @else if (answer().trim()) {
        <hl-markdown class="min-h-64 rounded-md border p-3" [markdown]="answer()" />
      } @else {
        <p class="min-h-64 rounded-md border border-dashed p-3 text-muted-foreground">
          Nothing to preview yet.
        </p>
      }

      <p class="text-muted-foreground" [id]="id + '-hint'">
        You can use Markdown. {{ answer().length | number }} of {{ maxLength | number }} characters.
      </p>
    </div>
  `,
})
export class OpenQuestionCandidate implements CandidateTaskComponent<
  OpenQuestionContent,
  OpenQuestionResponse | null
> {
  readonly content = input.required<OpenQuestionContent>();
  readonly response = input.required<OpenQuestionResponse | null>();
  readonly responseChange = output<OpenQuestionResponse>();

  protected readonly id = `hl-open-question-answer-${nextId++}`;
  protected readonly maxLength = answerMaxLength;
  protected readonly views: readonly { value: AnswerView; label: string }[] = [
    { value: 'write', label: 'Write' },
    { value: 'preview', label: 'Preview' },
  ];
  protected readonly view = signal<AnswerView>('write');
  protected readonly answer = computed(() => this.response()?.answer ?? '');
}
