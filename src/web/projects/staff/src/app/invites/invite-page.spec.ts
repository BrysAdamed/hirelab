import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { fireEvent, screen } from '@testing-library/angular';
import { STAFF_API_FETCH } from '../api/staff-api';
import { routes } from '../app.routes';
import { provideAuth } from '../auth/provide-auth';
import { AuthService } from '../auth/auth.service';
import { FakeFetch, fakeUser, json, noContent } from '../testing/fake-fetch';

describe('InvitePage', () => {
  let fake: FakeFetch;

  const preview = {
    email: 'nora@example.com',
    displayName: 'Nora Newmanager',
    role: 'Manager',
    teamName: 'Fake Team Beta',
    isNewTeam: true,
    expiresAt: '2026-01-08T09:00:00+00:00',
  };

  async function openInvite(url = '/invite#fake-token'): Promise<void> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideAuth(),
        { provide: STAFF_API_FETCH, useValue: fake.fetch },
      ],
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
  }

  function choosePassword(password: string, confirmation = password): void {
    fireEvent.input(screen.getByLabelText('Password'), { target: { value: password } });
    fireEvent.input(screen.getByLabelText('Confirm password'), {
      target: { value: confirmation },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Create account' }));
  }

  function posted(path: string): Request | undefined {
    return fake.requests.find(
      (request) => request.method === 'POST' && new URL(request.url).pathname === path,
    );
  }

  beforeEach(() => {
    fake = new FakeFetch()
      .on('GET', '/api/staff/auth/me', () => json(401, { status: 401 }))
      .on('GET', '/api/staff/auth/antiforgery', noContent)
      .on('POST', '/api/staff/invites/preview', () => json(200, preview));
  });

  it('shows the role and team of the invite, sending the token from the fragment', async () => {
    // Act
    await openInvite();

    // Assert
    expect(
      await screen.findByText(/invited to HireLab as the manager of a new team, Fake Team Beta/),
    ).toBeTruthy();
    expect(await posted('/api/staff/invites/preview')?.json()).toEqual({ token: 'fake-token' });
  });

  it('creates the account, signs the user in and goes home', async () => {
    // Arrange
    fake.on('POST', '/api/staff/invites/redeem', () => json(200, fakeUser('Manager')));
    await openInvite();
    await screen.findByLabelText('Password');

    // Act
    choosePassword('fake-password-not-a-secret');

    // Assert
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/'));
    expect(await posted('/api/staff/invites/redeem')?.json()).toEqual({
      token: 'fake-token',
      password: 'fake-password-not-a-secret',
    });
    expect(TestBed.inject(AuthService).user()?.role).toBe('Manager');
  });

  it('checks that the passwords match before calling the API', async () => {
    // Arrange
    await openInvite();
    await screen.findByLabelText('Password');

    // Act
    choosePassword('fake-password-not-a-secret', 'another-password-not-a-secret');

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('The passwords don’t match.');
    expect(posted('/api/staff/invites/redeem')).toBeUndefined();
  });

  it('explains why a link no longer works', async () => {
    // Arrange
    fake.on('POST', '/api/staff/invites/preview', () =>
      json(409, { status: 409, detail: 'This invite link has expired. Ask for a new one.' }),
    );

    // Act
    await openInvite();

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain(
      'This invite link has expired. Ask for a new one.',
    );
    expect(screen.queryByLabelText('Password')).toBeNull();
  });

  it('reports a link without a token without calling the API', async () => {
    // Act
    await openInvite('/invite');

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('incomplete');
    expect(posted('/api/staff/invites/preview')).toBeUndefined();
  });
});
