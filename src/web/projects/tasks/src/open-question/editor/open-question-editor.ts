import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import type { TaskContent } from '@hirelab/tasks/framework';
import type { EditorTaskComponent, TaskContentErrors } from '@hirelab/tasks/editor';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { HlmTextarea } from '@hirelab/ui/helm/textarea';
import {
  type OpenQuestionContent,
  type OpenQuestionStaffContent,
  promptMaxLength,
} from '../candidate/open-question';

const promptErrorKeys = ['candidateContent.prompt', 'candidateContent', 'staffContent'];

let nextId = 0;

@Component({
  selector: 'hl-open-question-editor',
  imports: [HlmLabel, HlmTextarea],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-1.5' },
  template: `
    <label hlmLabel [for]="id">Prompt</label>
    <textarea
      #promptInput
      hlmTextarea
      class="min-h-40 font-mono"
      required
      [id]="id"
      [value]="content().candidate.prompt"
      [attr.maxlength]="maxLength"
      [forceInvalid]="promptErrors().length > 0"
      [aria-describedby]="id + '-hint' + (promptErrors().length > 0 ? ' ' + id + '-errors' : '')"
      (input)="changePrompt(promptInput.value)"
    ></textarea>
    <p class="text-muted-foreground" [id]="id + '-hint'">
      The question the candidate answers, in Markdown.
    </p>
    @if (promptErrors().length > 0) {
      <ul class="text-destructive" [id]="id + '-errors'">
        @for (error of promptErrors(); track error) {
          <li>{{ error }}</li>
        }
      </ul>
    }
  `,
})
export class OpenQuestionEditor implements EditorTaskComponent<
  OpenQuestionContent,
  OpenQuestionStaffContent
> {
  readonly content = input.required<TaskContent<OpenQuestionContent, OpenQuestionStaffContent>>();
  readonly errors = input<TaskContentErrors>({});
  readonly contentChange = output<TaskContent<OpenQuestionContent, OpenQuestionStaffContent>>();

  protected readonly id = `hl-open-question-prompt-${nextId++}`;
  protected readonly maxLength = promptMaxLength;
  protected readonly promptErrors = computed(() =>
    promptErrorKeys.flatMap((key) => this.errors()[key] ?? []),
  );

  protected changePrompt(prompt: string): void {
    this.contentChange.emit({ candidate: { prompt }, staff: {} });
  }
}
