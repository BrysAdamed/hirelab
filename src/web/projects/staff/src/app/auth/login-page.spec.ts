import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { fireEvent, screen } from '@testing-library/angular';
import { STAFF_API_FETCH } from '../api/staff-api';
import { routes } from '../app.routes';
import { provideAuth } from './provide-auth';
import { FakeFetch, fakeUser, json, noContent } from '../testing/fake-fetch';

describe('LoginPage', () => {
  let fake: FakeFetch;

  async function openLogin(url = '/login'): Promise<void> {
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

  function logIn(email: string, password: string): void {
    fireEvent.input(screen.getByLabelText('Email'), { target: { value: email } });
    fireEvent.input(screen.getByLabelText('Password'), { target: { value: password } });
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }));
  }

  beforeEach(() => {
    fake = new FakeFetch()
      .on('GET', '/api/staff/auth/me', () => json(401, { status: 401 }))
      .on('GET', '/api/staff/auth/antiforgery', noContent);
  });

  it('gets an antiforgery token when it opens', async () => {
    // Act
    await openLogin();

    // Assert
    expect(fake.requests.map((request) => new URL(request.url).pathname)).toContain(
      '/api/staff/auth/antiforgery',
    );
  });

  it('logs in and goes back to where the user was going', async () => {
    // Arrange
    fake.on('POST', '/api/staff/auth/login', () => json(200, fakeUser('Manager')));
    await openLogin('/login?returnUrl=%2Ftasks');

    // Act
    logIn('manager@example.com', 'fake-password-not-a-secret');

    // Assert
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/tasks'));
    const login = fake.requests.find((request) => request.method === 'POST');
    expect(await login?.json()).toEqual({
      email: 'manager@example.com',
      password: 'fake-password-not-a-secret',
    });
  });

  it('waits for the antiforgery token before it sends the login', async () => {
    // Arrange
    let releaseToken!: () => void;
    const tokenIssued = new Promise<void>((resolve) => (releaseToken = resolve));
    fake
      .on('GET', '/api/staff/auth/antiforgery', async () => {
        await tokenIssued;
        return noContent();
      })
      .on('POST', '/api/staff/auth/login', () => json(200, fakeUser('Manager')));
    await openLogin();

    // Act
    logIn('manager@example.com', 'fake-password-not-a-secret');
    await new Promise((resolve) => setTimeout(resolve, 20));
    const postedBeforeToken = fake.requests.some((request) => request.method === 'POST');
    releaseToken();

    // Assert
    expect(postedBeforeToken).toBe(false);
    await vi.waitFor(() =>
      expect(fake.requests.some((request) => request.method === 'POST')).toBe(true),
    );
  });

  it('shows the API message when login fails', async () => {
    // Arrange
    fake.on('POST', '/api/staff/auth/login', () =>
      json(400, { status: 400, detail: 'The email or password is incorrect.' }),
    );
    await openLogin();

    // Act
    logIn('manager@example.com', 'wrong-password-not-a-secret');

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain(
      'The email or password is incorrect.',
    );
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('asks for both fields before calling the API', async () => {
    // Arrange
    await openLogin();

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain(
      'Enter your email and password.',
    );
    expect(fake.requests.some((request) => request.method === 'POST')).toBe(false);
  });
});
