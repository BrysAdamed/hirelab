import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleAlert, lucideLoaderCircle } from '@ng-icons/lucide';
import { ThemeSwitcher } from '@hirelab/ui';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { safeReturnUrl } from './auth.guards';
import { AuthService } from './auth.service';

@Component({
  selector: 'hl-login-page',
  imports: [
    ReactiveFormsModule,
    NgIcon,
    ThemeSwitcher,
    HlmButton,
    HlmCardImports,
    HlmInput,
    HlmLabel,
  ],
  providers: [provideIcons({ lucideCircleAlert, lucideLoaderCircle })],
  templateUrl: './login-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', Validators.required],
    password: ['', Validators.required],
  });

  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    void this.auth.prepareLogin();
  }

  protected async submit(): Promise<void> {
    if (this.pending()) {
      return;
    }
    if (this.form.invalid) {
      this.error.set('Enter your email and password.');
      return;
    }

    this.pending.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    const result = await this.auth.login(email, password);
    this.pending.set(false);

    if (result.ok) {
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
      await this.router.navigateByUrl(safeReturnUrl(returnUrl));
    } else {
      this.error.set(result.message);
      this.form.controls.password.reset();
    }
  }
}
