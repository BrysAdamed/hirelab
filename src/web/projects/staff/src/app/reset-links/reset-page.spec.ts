import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { fireEvent, screen } from '@testing-library/angular';
import { STAFF_API_FETCH } from '../api/staff-api';
import { routes } from '../app.routes';
import { provideAuth } from '../auth/provide-auth';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';

describe('ResetPage', () => {
  let fake: FakeFetch;

  const preview = {
    email: 'ivy@example.com',
    displayName: 'Ivy Interviewer',
    expiresAt: '2026-01-06T09:00:00+00:00',
  };

  async function openReset(url = '/reset#fake-token'): Promise<void> {
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
    fireEvent.input(screen.getByLabelText('New password'), { target: { value: password } });
    fireEvent.input(screen.getByLabelText('Confirm new password'), {
      target: { value: confirmation },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Set password' }));
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
      .on('POST', '/api/staff/reset-links/preview', () => json(200, preview));
  });

  it('shows whose password the link sets, sending the token from the fragment', async () => {
    // Act
    await openReset();

    // Assert
    expect(await screen.findByText(/Ivy Interviewer, choose a new password/)).toBeTruthy();
    expect(await posted('/api/staff/reset-links/preview')?.json()).toEqual({
      token: 'fake-token',
    });
  });

  it('sets the password and offers to log in', async () => {
    // Arrange
    fake.on('POST', '/api/staff/reset-links/redeem', noContent);
    await openReset();
    await screen.findByLabelText('New password');

    // Act
    choosePassword('fake-password-not-a-secret');

    // Assert
    expect((await screen.findByRole('status')).textContent).toContain('Your password is changed');
    expect(screen.getByRole('link', { name: 'Log in' }).getAttribute('href')).toBe('/login');
    expect(await posted('/api/staff/reset-links/redeem')?.json()).toEqual({
      token: 'fake-token',
      password: 'fake-password-not-a-secret',
    });
  });

  it('checks that the passwords match before calling the API', async () => {
    // Arrange
    await openReset();
    await screen.findByLabelText('New password');

    // Act
    choosePassword('fake-password-not-a-secret', 'another-password-not-a-secret');

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('The passwords don’t match.');
    expect(posted('/api/staff/reset-links/redeem')).toBeUndefined();
  });

  it('explains why a link no longer works', async () => {
    // Arrange
    fake.on('POST', '/api/staff/reset-links/preview', () =>
      json(409, { status: 409, detail: 'This reset link has already been used.' }),
    );

    // Act
    await openReset();

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain(
      'This reset link has already been used.',
    );
    expect(screen.queryByLabelText('New password')).toBeNull();
  });

  it('reports a link without a token without calling the API', async () => {
    // Act
    await openReset('/reset');

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('incomplete');
    expect(posted('/api/staff/reset-links/preview')).toBeUndefined();
  });
});
