import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideKeyRound, lucideLoaderCircle } from '@ng-icons/lucide';
import { CopyButton } from '@hirelab/ui/copy';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { type ResetLinkKind, ResetLinksService } from './reset-links.service';

/**
 * A user list's "Reset password" action: after confirmation it generates a reset link for the user and shows it once,
 * to be copied and sent by hand. Generating a link revokes the user's earlier unused one.
 */
@Component({
  selector: 'hl-reset-link-action',
  imports: [DatePipe, NgIcon, CopyButton, HlmButton, HlmDialogImports, HlmInput, HlmLabel],
  providers: [provideIcons({ lucideCircleAlert, lucideKeyRound, lucideLoaderCircle })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hlm-dialog (closed)="clear()">
      <button
        hlmDialogTrigger
        hlmBtn
        type="button"
        variant="outline"
        size="sm"
        [attr.aria-label]="'Reset the password of ' + displayName()"
      >
        <ng-icon name="lucideKeyRound" aria-hidden="true" />
        Reset password
      </button>
      <ng-template hlmDialogPortal>
        <hlm-dialog-content>
          <hlm-dialog-header>
            <h3 hlmDialogTitle>Reset password</h3>
            <p hlmDialogDescription>
              Generate a link for {{ displayName() }} to set a new password. It works once, for 24
              hours. An earlier unused link stops working.
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

          @if (issued(); as issued) {
            <div class="flex flex-col gap-2" role="status">
              <p>
                Copy the link now and send it by hand: it isn't shown again. It expires
                {{ issued.expiresAt | date: 'medium' }}.
              </p>
              <div class="flex flex-wrap items-center gap-2">
                <label hlmLabel class="sr-only" [for]="linkId()">Reset link</label>
                <input
                  hlmInput
                  readonly
                  class="flex-1 font-mono"
                  [id]="linkId()"
                  [value]="issued.url"
                />
                <hl-copy-button [text]="issued.url" label="Copy link" />
              </div>
            </div>
          }

          <hlm-dialog-footer>
            @if (issued()) {
              <button hlmBtn hlmDialogClose type="button">Done</button>
            } @else {
              <button hlmBtn hlmDialogClose type="button" variant="outline">Cancel</button>
              <button hlmBtn type="button" [disabled]="pending()" (click)="generate()">
                @if (pending()) {
                  <ng-icon
                    name="lucideLoaderCircle"
                    class="motion-safe:animate-spin"
                    aria-hidden="true"
                  />
                }
                Generate link
              </button>
            }
          </hlm-dialog-footer>
        </hlm-dialog-content>
      </ng-template>
    </hlm-dialog>
  `,
})
export class ResetLinkAction {
  private readonly resetLinks = inject(ResetLinksService);
  private readonly origin = inject(DOCUMENT).location.origin;

  readonly userId = input.required<string>();
  readonly displayName = input.required<string>();
  readonly kind = input.required<ResetLinkKind>();

  protected readonly linkId = computed(() => `reset-link-${this.userId()}`);
  protected readonly issued = signal<{ url: string; expiresAt: string } | null>(null);
  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async generate(): Promise<void> {
    if (this.pending()) {
      return;
    }
    this.pending.set(true);
    this.error.set(null);
    const result = await this.resetLinks.generate(this.kind(), this.userId());
    this.pending.set(false);

    if (result.ok) {
      this.issued.set({
        url: new URL(result.value.linkPath, this.origin).toString(),
        expiresAt: result.value.expiresAt,
      });
    } else {
      this.error.set(result.message);
    }
  }

  protected clear(): void {
    this.issued.set(null);
    this.error.set(null);
  }
}
