import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCircleAlert,
  lucideLink,
  lucideLoaderCircle,
  lucideRefreshCw,
} from '@ng-icons/lucide';
import { CopyButton } from '@hirelab/ui/copy';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { SessionsService } from './sessions.service';

/**
 * The session page's "Create candidate link" / "Regenerate link" action (ADR 0008, ADR 0031). Only the link's hash is
 * stored, so the link is shown once, right after it is created, to be copied and sent by hand. Regenerating gives a
 * new link and stops the old one, including for a candidate who already opened it.
 */
@Component({
  selector: 'hl-candidate-link-action',
  imports: [NgIcon, CopyButton, HlmButton, HlmDialogImports, HlmInput, HlmLabel],
  providers: [provideIcons({ lucideCircleAlert, lucideLink, lucideLoaderCircle, lucideRefreshCw })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hlm-dialog (closed)="close()">
      <button
        hlmDialogTrigger
        hlmBtn
        type="button"
        [variant]="hasLink() ? 'outline' : 'default'"
        size="sm"
      >
        <ng-icon [name]="hasLink() ? 'lucideRefreshCw' : 'lucideLink'" aria-hidden="true" />
        {{ hasLink() ? 'Regenerate link' : 'Create candidate link' }}
      </button>
      <ng-template hlmDialogPortal>
        <hlm-dialog-content>
          <hlm-dialog-header>
            <h3 hlmDialogTitle>
              @if (url()) {
                Candidate link for Session {{ sessionNumber() }}
              } @else if (hasLink()) {
                Regenerate the candidate link
              } @else {
                Create the candidate link
              }
            </h3>
            <p hlmDialogDescription>
              @if (url()) {
                Copy the link now and send it to the candidate by hand: it isn’t shown again.
              } @else if (hasLink()) {
                You get a new link for Session {{ sessionNumber() }}. The old link stops working,
                also for a candidate who already opened it.
              } @else {
                The candidate opens Session {{ sessionNumber() }} with this link. Before the session
                goes live it shows a waiting page; it stops working when the session finishes.
              }
            </p>
          </hlm-dialog-header>

          @if (error(); as message) {
            <div
              role="alert"
              class="flex items-start gap-2 rounded-md border border-destructive p-2"
            >
              <ng-icon
                name="lucideCircleAlert"
                size="16"
                class="mt-0.5 shrink-0 text-destructive"
                aria-hidden="true"
              />
              <p>{{ message }}</p>
            </div>
          }

          @if (url(); as url) {
            <div class="flex flex-col gap-2" role="status">
              <div class="flex flex-wrap items-center gap-2">
                <label hlmLabel class="sr-only" [for]="linkId()">Candidate link</label>
                <input hlmInput readonly class="flex-1 font-mono" [id]="linkId()" [value]="url" />
                <hl-copy-button [text]="url" label="Copy link" />
              </div>
            </div>
          }

          <hlm-dialog-footer>
            @if (url()) {
              <button hlmBtn hlmDialogClose type="button">Done</button>
            } @else {
              <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
              <button hlmBtn type="button" [disabled]="pending()" (click)="issue()">
                @if (pending()) {
                  <ng-icon
                    name="lucideLoaderCircle"
                    class="motion-safe:animate-spin"
                    aria-hidden="true"
                  />
                }
                {{ hasLink() ? 'Regenerate link' : 'Create link' }}
              </button>
            }
          </hlm-dialog-footer>
        </hlm-dialog-content>
      </ng-template>
    </hlm-dialog>
  `,
})
export class CandidateLinkAction {
  private readonly sessionsService = inject(SessionsService);
  private readonly origin = inject(DOCUMENT).location.origin;

  readonly sessionId = input.required<string>();
  readonly sessionNumber = input.required<number>();
  /** When the current link was created; `null` while the session has none. */
  readonly linkCreatedAt = input.required<string | null>();
  /** Emitted when the dialog closes after a link was created, so the page shows its new creation time. */
  readonly issued = output<void>();

  protected readonly linkId = computed(() => `candidate-link-${this.sessionId()}`);
  protected readonly url = signal<string | null>(null);
  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly hasLink = computed(() => this.linkCreatedAt() !== null);

  protected async issue(): Promise<void> {
    if (this.pending()) {
      return;
    }
    this.pending.set(true);
    this.error.set(null);
    const result = await this.sessionsService.issueCandidateLink(this.sessionId());
    this.pending.set(false);

    if (result.ok) {
      this.url.set(new URL(result.value.linkPath, this.origin).toString());
    } else {
      this.error.set(result.message);
    }
  }

  protected close(): void {
    const issued = this.url() !== null;
    this.url.set(null);
    this.error.set(null);
    if (issued) {
      this.issued.emit();
    }
  }
}
