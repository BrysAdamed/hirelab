import { DatePipe, DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideDownload, lucidePaperclip, lucideUpload } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import type { Attachment } from '../api/staff-api';
import {
  AttachmentsService,
  acceptedAttachmentTypes,
  maxAttachmentsPerCandidate,
} from './attachments.service';
import { DeleteAttachmentAction } from './delete-attachment-action';

/** "12 KB", "1.5 MB": a file size for people. */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  if (bytes < 1024 * 1024) {
    return `${Math.round(bytes / 1024)} KB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * A candidate's attachments card: the files with their size and who uploaded them when, download for everyone, and
 * upload and delete while the candidate can be changed.
 */
@Component({
  selector: 'hl-candidate-attachments',
  imports: [DatePipe, NgIcon, HlmButton, HlmCardImports, DeleteAttachmentAction],
  providers: [provideIcons({ lucideCircleAlert, lucideDownload, lucidePaperclip, lucideUpload })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section hlmCard aria-labelledby="attachments-title">
      <div hlmCardHeader class="flex flex-row flex-wrap items-center justify-between gap-2">
        <div class="flex flex-col gap-1">
          <h2 hlmCardTitle id="attachments-title">Attachments</h2>
          @if (canChange()) {
            <p class="text-muted-foreground">
              {{ attachments().length }} of {{ maxFiles }} files · PDF, DOCX, TXT, MD, PNG or JPG up
              to 10 MB
            </p>
          }
        </div>
        @if (canChange()) {
          <input
            #fileInput
            type="file"
            class="hidden"
            [accept]="acceptedTypes"
            (change)="upload($event)"
          />
          <button
            hlmBtn
            type="button"
            size="sm"
            variant="outline"
            [disabled]="pending() || attachments().length >= maxFiles"
            (click)="fileInput.click()"
          >
            <ng-icon name="lucideUpload" aria-hidden="true" />
            {{ uploading() ? 'Uploading…' : 'Upload file' }}
          </button>
        }
      </div>
      <div hlmCardContent class="flex flex-col gap-3">
        @if (error(); as message) {
          <div role="alert" class="flex items-start gap-2 rounded-md border border-destructive p-2">
            <ng-icon
              name="lucideCircleAlert"
              size="16"
              class="mt-0.5 shrink-0 text-destructive"
              aria-hidden="true"
            />
            <p>{{ message }}</p>
          </div>
        }
        @if (loaded()) {
          @if (attachments().length === 0) {
            <p
              class="flex items-center justify-center gap-2 rounded-md border border-dashed p-3 text-muted-foreground"
            >
              <ng-icon name="lucidePaperclip" size="16" aria-hidden="true" />
              {{ canChange() ? 'No attachments yet.' : 'No attachments.' }}
            </p>
          } @else {
            <div class="overflow-x-auto rounded-md border">
              <table class="w-full text-left">
                <caption class="sr-only">
                  Attachments
                </caption>
                <thead class="border-b bg-muted text-muted-foreground">
                  <tr>
                    <th scope="col" class="px-3 py-2 font-medium">Name</th>
                    <th scope="col" class="px-3 py-2 font-medium">Size</th>
                    <th scope="col" class="px-3 py-2 font-medium">Uploaded</th>
                    <th scope="col" class="px-3 py-2 font-medium">
                      <span class="sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  @for (attachment of attachments(); track attachment.id) {
                    <tr class="border-b last:border-b-0">
                      <td class="px-3 py-2 break-all">{{ attachment.fileName }}</td>
                      <td class="px-3 py-2 whitespace-nowrap">{{ size(attachment.size) }}</td>
                      <td class="px-3 py-2">
                        {{ attachment.uploadedAt | date: 'medium' }}
                        <span class="text-muted-foreground">
                          by {{ attachment.uploadedByName ?? 'a former user' }}
                        </span>
                      </td>
                      <td class="px-3 py-2">
                        <div class="flex justify-end gap-1">
                          <button
                            hlmBtn
                            type="button"
                            variant="ghost"
                            size="sm"
                            [attr.aria-label]="'Download ' + attachment.fileName"
                            (click)="download(attachment)"
                          >
                            <ng-icon name="lucideDownload" aria-hidden="true" />
                          </button>
                          @if (canChange()) {
                            <hl-delete-attachment-action
                              [fileName]="attachment.fileName"
                              [disabled]="pending()"
                              (confirmed)="delete(attachment)"
                            />
                          }
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        }
      </div>
    </section>
  `,
})
export class CandidateAttachments {
  private readonly attachmentsService = inject(AttachmentsService);
  private readonly document = inject(DOCUMENT);
  private latestRequest = 0;

  readonly candidateId = input.required<string>();
  /** Whether files may be uploaded and deleted: the position is open and the candidate isn't anonymised. */
  readonly canChange = input(false);
  /** The candidate's version: the files are read again when it changes, for example after anonymisation deleted them. */
  readonly candidateVersion = input(0);

  protected readonly maxFiles = maxAttachmentsPerCandidate;
  protected readonly acceptedTypes = acceptedAttachmentTypes;
  protected readonly attachments = signal<Attachment[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly uploading = signal(false);
  protected readonly deleting = signal(false);
  protected readonly pending = computed(() => this.uploading() || this.deleting());

  protected readonly size = formatFileSize;

  private readonly source = computed(() => ({
    candidateId: this.candidateId(),
    version: this.candidateVersion(),
  }));

  constructor() {
    effect(() => void this.load(this.source().candidateId));
  }

  protected async upload(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.uploading.set(true);
    const result = await this.attachmentsService.upload(this.candidateId(), file);
    this.uploading.set(false);
    input.value = '';
    if (result.ok) {
      await this.load(this.candidateId());
    } else {
      this.error.set(result.message);
    }
  }

  protected async download(attachment: Attachment): Promise<void> {
    const result = await this.attachmentsService.download(this.candidateId(), attachment.id);
    if (!result.ok) {
      this.error.set(result.message);
      return;
    }

    this.error.set(null);
    const url = URL.createObjectURL(result.value);
    const link = this.document.createElement('a');
    link.href = url;
    link.download = attachment.fileName;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url));
  }

  protected async delete(attachment: Attachment): Promise<void> {
    this.deleting.set(true);
    const result = await this.attachmentsService.delete(this.candidateId(), attachment.id);
    this.deleting.set(false);
    if (result.ok) {
      await this.load(this.candidateId());
    } else {
      this.error.set(result.message);
    }
  }

  private async load(candidateId: string): Promise<void> {
    const request = ++this.latestRequest;
    const result = await this.attachmentsService.list(candidateId);
    if (request !== this.latestRequest) {
      return;
    }
    this.loaded.set(true);
    if (result.ok) {
      this.error.set(null);
      this.attachments.set(result.value);
    } else {
      this.error.set(result.message);
    }
  }
}
