import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import type { TaskContent } from '@hirelab/tasks/framework';
import type { EditorTaskComponent, TaskContentErrors } from '@hirelab/tasks/editor';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { HlmTextarea } from '@hirelab/ui/helm/textarea';
import {
  type ChangedFile,
  type CodeReviewContent,
  type CodeReviewStaffContent,
  type ExpectedFinding,
  codeReviewLimits,
} from '../candidate/code-review';
import { ChangedFileEditor } from './changed-file-editor';
import { ExpectedFindingEditor } from './expected-finding-editor';
import { FieldErrors, describedBy, messagesOf } from './field-errors';

type CodeReviewTaskContent = TaskContent<CodeReviewContent, CodeReviewStaffContent>;

let nextId = 0;

@Component({
  selector: 'hl-code-review-editor',
  imports: [
    ChangedFileEditor,
    ExpectedFindingEditor,
    FieldErrors,
    HlmButton,
    HlmInput,
    HlmLabel,
    HlmTextarea,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-8' },
  template: `
    <hl-field-errors [fieldId]="id" [messages]="contentErrors()" />

    <section class="flex flex-col gap-4" [attr.aria-labelledby]="id + '-pull-request'">
      <h2 class="text-lg font-semibold" [id]="id + '-pull-request'">Pull request</h2>
      <div class="flex flex-col gap-1.5">
        <label hlmLabel [for]="id + '-title'">Title</label>
        <input
          #titleInput
          hlmInput
          required
          [id]="id + '-title'"
          [value]="value().candidate.title"
          [attr.maxlength]="limits.titleMaxLength"
          [forceInvalid]="titleErrors().length > 0"
          [aria-describedby]="describedBy(id + '-title', titleErrors())"
          (input)="changeCandidate({ title: titleInput.value })"
        />
        <hl-field-errors [fieldId]="id + '-title'" [messages]="titleErrors()" />
      </div>
      <div class="flex flex-col gap-1.5">
        <label hlmLabel [for]="id + '-description'">Description</label>
        <textarea
          #descriptionInput
          hlmTextarea
          class="min-h-32 font-mono"
          [id]="id + '-description'"
          [value]="value().candidate.description"
          [attr.maxlength]="limits.descriptionMaxLength"
          [forceInvalid]="descriptionErrors().length > 0"
          [aria-describedby]="describedBy(id + '-description', descriptionErrors(), true)"
          (input)="changeCandidate({ description: descriptionInput.value })"
        ></textarea>
        <p class="text-muted-foreground text-sm" [id]="id + '-description-hint'">
          What the pull request changes and why, in Markdown.
        </p>
        <hl-field-errors [fieldId]="id + '-description'" [messages]="descriptionErrors()" />
      </div>
    </section>

    <section class="flex flex-col gap-4" [attr.aria-labelledby]="id + '-files'">
      <h2 class="text-lg font-semibold" [id]="id + '-files'">Changed files</h2>
      <hl-field-errors [fieldId]="id + '-files'" [messages]="filesErrors()" />
      @for (file of value().candidate.files; track $index) {
        <hl-changed-file-editor
          [file]="file"
          [index]="$index"
          [errors]="errors()"
          (fileChange)="changeFile($index, $event)"
          (remove)="removeFile($index)"
        />
      }
      <div>
        <button
          hlmBtn
          type="button"
          variant="outline"
          [disabled]="value().candidate.files.length >= limits.maxFiles"
          (click)="addFile()"
        >
          Add changed file
        </button>
      </div>
    </section>

    <section class="flex flex-col gap-4" [attr.aria-labelledby]="id + '-findings'">
      <h2 class="text-lg font-semibold" [id]="id + '-findings'">Expected findings</h2>
      <p class="text-muted-foreground text-sm">
        The issues you planted. Interviewers see them next to the candidate's review comments; the
        candidate never does.
      </p>
      <hl-field-errors [fieldId]="id + '-findings'" [messages]="findingsErrors()" />
      @for (finding of value().staff.expectedFindings; track $index) {
        <hl-expected-finding-editor
          [finding]="finding"
          [index]="$index"
          [paths]="paths()"
          [errors]="errors()"
          (findingChange)="changeFinding($index, $event)"
          (remove)="removeFinding($index)"
        />
      }
      <div>
        <button
          hlmBtn
          type="button"
          variant="outline"
          [disabled]="value().staff.expectedFindings.length >= limits.maxExpectedFindings"
          (click)="addFinding()"
        >
          Add expected finding
        </button>
      </div>
    </section>
  `,
})
export class CodeReviewEditor implements EditorTaskComponent<
  CodeReviewContent,
  CodeReviewStaffContent
> {
  readonly content = input.required<CodeReviewTaskContent>();
  readonly errors = input<TaskContentErrors>({});
  readonly contentChange = output<CodeReviewTaskContent>();

  protected readonly id = `hl-code-review-${nextId++}`;
  protected readonly limits = codeReviewLimits;
  protected readonly describedBy = describedBy;

  protected readonly value = computed<CodeReviewTaskContent>(() => {
    const { candidate, staff } = this.content();
    return {
      candidate: {
        title: candidate?.title ?? '',
        description: candidate?.description ?? '',
        files: candidate?.files ?? [],
      },
      staff: { expectedFindings: staff?.expectedFindings ?? [] },
    };
  });

  protected readonly paths = computed(() => [
    ...new Set(
      this.value()
        .candidate.files.map((file) => file.path)
        .filter((path) => path),
    ),
  ]);

  protected readonly contentErrors = computed(() =>
    messagesOf(this.errors(), 'candidateContent', 'staffContent'),
  );
  protected readonly titleErrors = computed(() =>
    messagesOf(this.errors(), 'candidateContent.title'),
  );
  protected readonly descriptionErrors = computed(() =>
    messagesOf(this.errors(), 'candidateContent.description'),
  );
  protected readonly filesErrors = computed(() =>
    messagesOf(this.errors(), 'candidateContent.files'),
  );
  protected readonly findingsErrors = computed(() =>
    messagesOf(this.errors(), 'staffContent.expectedFindings'),
  );

  protected changeCandidate(change: Partial<CodeReviewContent>): void {
    const { candidate, staff } = this.value();
    this.contentChange.emit({ candidate: { ...candidate, ...change }, staff });
  }

  protected addFile(): void {
    const files = this.value().candidate.files;
    this.changeCandidate({
      files: [...files, { path: '', language: 'plaintext', original: '', modified: '' }],
    });
  }

  protected changeFile(index: number, file: ChangedFile): void {
    const { candidate, staff } = this.value();
    const oldPath = candidate.files[index].path;
    const files = candidate.files.map((current, i) => (i === index ? file : current));
    const otherPaths = files.filter((_, i) => i !== index).map((other) => other.path);
    const followsRename =
      file.path !== oldPath && !otherPaths.includes(oldPath) && !otherPaths.includes(file.path);
    this.contentChange.emit({
      candidate: { ...candidate, files },
      staff: followsRename
        ? {
            expectedFindings: staff.expectedFindings.map((finding) =>
              finding.file === oldPath ? { ...finding, file: file.path } : finding,
            ),
          }
        : staff,
    });
  }

  protected removeFile(index: number): void {
    this.changeCandidate({ files: this.value().candidate.files.filter((_, i) => i !== index) });
  }

  protected addFinding(): void {
    const findings = this.value().staff.expectedFindings;
    this.changeStaff([
      ...findings,
      {
        id: this.unusedFindingId(findings),
        file: this.paths()[0] ?? '',
        side: 'modified',
        startLine: 1,
        endLine: 1,
        title: '',
        description: '',
        severity: 'Major',
      },
    ]);
  }

  protected changeFinding(index: number, finding: ExpectedFinding): void {
    this.changeStaff(
      this.value().staff.expectedFindings.map((current, i) => (i === index ? finding : current)),
    );
  }

  protected removeFinding(index: number): void {
    this.changeStaff(this.value().staff.expectedFindings.filter((_, i) => i !== index));
  }

  private changeStaff(expectedFindings: readonly ExpectedFinding[]): void {
    this.contentChange.emit({ candidate: this.value().candidate, staff: { expectedFindings } });
  }

  private unusedFindingId(findings: readonly ExpectedFinding[]): string {
    const ids = new Set(findings.map((finding) => finding.id));
    let number = findings.length + 1;
    while (ids.has(`finding-${number}`)) {
      number++;
    }
    return `finding-${number}`;
  }
}
