import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  type OnInit,
} from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideCircleCheck, lucideLoaderCircle } from '@ng-icons/lucide';
import { ThemeSwitcher } from '@hirelab/ui';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { LinkRedemption } from '../one-time-links/link-redemption';
import { ResetLinksService } from './reset-links.service';

/**
 * Redeems a reset link (`/reset#<token>`). Anonymous and outside the app shell: the user sets a new password, which
 * ends their other sessions, and then logs in with it.
 */
@Component({
  selector: 'hl-reset-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NgIcon,
    ThemeSwitcher,
    HlmButton,
    HlmCardImports,
    HlmInput,
    HlmLabel,
  ],
  providers: [provideIcons({ lucideCircleAlert, lucideCircleCheck, lucideLoaderCircle })],
  templateUrl: './reset-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPage implements OnInit {
  private readonly redemption = new LinkRedemption(inject(ResetLinksService), 'reset link');

  protected readonly minimumPasswordLength = this.redemption.minimumPasswordLength;
  protected readonly done = signal(false);
  protected readonly loading = this.redemption.loading;
  protected readonly link = computed(() => (this.done() ? null : this.redemption.preview()));
  protected readonly unavailable = this.redemption.unavailable;
  protected readonly pending = this.redemption.pending;
  protected readonly error = this.redemption.error;
  protected readonly form = this.redemption.form;

  async ngOnInit(): Promise<void> {
    await this.redemption.load();
  }

  protected async submit(): Promise<void> {
    if (await this.redemption.submit()) {
      this.done.set(true);
    }
  }
}
