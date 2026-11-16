import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { type CurrentUser, problemMessage, STAFF_API } from '../api/staff-api';

export type LoginResult = { ok: true; user: CurrentUser } | { ok: false; message: string };

const genericFailure = 'Logging in failed. Try again in a moment.';

/** The signed-in staff user and the login, logout and session-expiry flows. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(STAFF_API);
  private readonly router = inject(Router);

  private readonly _user = signal<CurrentUser | null>(null);
  private loaded: Promise<CurrentUser | null> | null = null;

  /** The signed-in user, or `null` before login and after logout. */
  readonly user = this._user.asReadonly();
  readonly isSignedIn = computed(() => this._user() !== null);

  /** Loads the current user once per app start; later calls reuse the answer until the user changes. */
  ensureLoaded(): Promise<CurrentUser | null> {
    this.loaded ??= this.fetchCurrentUser();
    return this.loaded;
  }

  /** Gets an antiforgery token for the current (usually anonymous) user, which login needs. */
  async prepareLogin(): Promise<void> {
    await this.api.GET('/api/staff/auth/antiforgery');
  }

  async login(email: string, password: string): Promise<LoginResult> {
    const { data, error } = await this.api.POST('/api/staff/auth/login', {
      body: { email, password },
    });
    if (data) {
      this.setUser(data);
      return { ok: true, user: data };
    }
    return { ok: false, message: problemMessage(error, genericFailure) };
  }

  /** Changes the signed-in user's password. This session continues; the user's other sessions end. */
  async changePassword(
    currentPassword: string,
    newPassword: string,
  ): Promise<{ ok: true } | { ok: false; message: string }> {
    const { error, response } = await this.api.POST('/api/staff/auth/change-password', {
      body: { currentPassword, newPassword },
    });
    return response.ok
      ? { ok: true }
      : { ok: false, message: problemMessage(error, 'Changing the password failed. Try again.') };
  }

  /** Records a user the API has just signed in another way, for example by redeeming an invite. */
  signedIn(user: CurrentUser): void {
    this.setUser(user);
  }

  async logout(): Promise<void> {
    await this.api.POST('/api/staff/auth/logout');
    this.setUser(null);
    await this.router.navigateByUrl('/login');
  }

  /** Called when an API call answers 401: forgets the user and sends them to login, then back here. */
  sessionExpired(): void {
    if (this.router.url.startsWith('/login')) {
      return;
    }
    this.setUser(null);
    void this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
  }

  private setUser(user: CurrentUser | null): void {
    this._user.set(user);
    this.loaded = Promise.resolve(user);
  }

  private async fetchCurrentUser(): Promise<CurrentUser | null> {
    const { data } = await this.api.GET('/api/staff/auth/me');
    this._user.set(data ?? null);
    return data ?? null;
  }
}
