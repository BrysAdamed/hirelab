import { ChangeDetectionStrategy, Component, DOCUMENT, inject, input, signal } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCheck, lucideCopy } from '@ng-icons/lucide';
import { HlmButton } from '@hirelab/ui/helm/button';

type CopyState = 'idle' | 'copied' | 'failed';

const resetAfterMs = 2000;

/**
 * Copies `text` to the clipboard and confirms it next to the button, in a polite live region.
 * Used for links that are sent by hand, such as invite and candidate links.
 */
@Component({
  selector: 'hl-copy-button',
  imports: [HlmButton, NgIcon],
  providers: [provideIcons({ lucideCheck, lucideCopy })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex items-center gap-2' },
  template: `
    <button hlmBtn type="button" variant="outline" size="sm" (click)="copy()">
      <ng-icon [name]="state() === 'copied' ? 'lucideCheck' : 'lucideCopy'" aria-hidden="true" />
      {{ label() }}
    </button>
    <span aria-live="polite" class="text-muted-foreground">
      @switch (state()) {
        @case ('copied') {
          Copied
        }
        @case ('failed') {
          Copying failed. Select the text and copy it.
        }
      }
    </span>
  `,
})
export class CopyButton {
  private readonly clipboard = inject(DOCUMENT).defaultView?.navigator.clipboard;
  private resetTimer: ReturnType<typeof setTimeout> | undefined;

  /** The text to copy. */
  readonly text = input.required<string>();
  readonly label = input('Copy');

  protected readonly state = signal<CopyState>('idle');

  protected async copy(): Promise<void> {
    clearTimeout(this.resetTimer);
    try {
      if (!this.clipboard) {
        throw new Error('The clipboard is not available.');
      }
      await this.clipboard.writeText(this.text());
      this.state.set('copied');
      this.resetTimer = setTimeout(() => this.state.set('idle'), resetAfterMs);
    } catch {
      this.state.set('failed');
    }
  }
}
