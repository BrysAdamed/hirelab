import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { fireEvent, screen, within } from '@testing-library/angular';
import { STAFF_API_FETCH, type StaffRole } from './api/staff-api';
import { routes } from './app.routes';
import { AuthService } from './auth/auth.service';
import { provideAuth } from './auth/provide-auth';
import { FakeFetch, fakeUser, json, noContent } from './testing/fake-fetch';

describe('staff app routes', () => {
  let fake: FakeFetch;

  async function start(url: string, role: StaffRole | null): Promise<RouterTestingHarness> {
    fake = new FakeFetch()
      .on('GET', '/api/staff/auth/me', () =>
        role ? json(200, fakeUser(role)) : json(401, { status: 401 }),
      )
      .on('GET', '/api/staff/auth/antiforgery', noContent)
      .on('POST', '/api/staff/auth/logout', noContent);
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideAuth(),
        { provide: STAFF_API_FETCH, useValue: fake.fetch },
      ],
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    return harness;
  }

  function navLinks(): string[] {
    const nav = screen.getByRole('navigation', { name: 'Main' });
    return within(nav)
      .getAllByRole('link')
      .map((link) => link.textContent?.trim() ?? '');
  }

  it('sends an anonymous visitor to login and remembers where they were going', async () => {
    // Act
    await start('/positions', null);

    // Assert
    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fpositions');
    expect(await screen.findByRole('heading', { name: 'Log in' })).toBeTruthy();
  });

  it.each<[StaffRole, string[]]>([
    ['Admin', ['Home', 'Teams', 'Users']],
    ['Manager', ['Home', 'Job positions', 'Task bank', 'My team']],
    ['Interviewer', ['Home', 'Job positions', 'Task bank']],
  ])('shows the %s the navigation of their role', async (role, expected) => {
    // Act
    await start('/', role);

    // Assert
    expect(navLinks()).toEqual(expected);
    expect(screen.getByRole('heading', { name: `Welcome, Fake ${role}` })).toBeTruthy();
  });

  it('sends a user to the home page when their role may not open a page', async () => {
    // Act
    await start('/users', 'Interviewer');

    // Assert
    expect(TestBed.inject(Router).url).toBe('/');
  });

  it('sends the user to login and back when their session ends', async () => {
    // Arrange
    await start('/tasks', 'Interviewer');

    // Act
    TestBed.inject(AuthService).sessionExpired();

    // Assert
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Ftasks'));
  });

  it('logs out from the user menu', async () => {
    // Arrange
    const harness = await start('/', 'Admin');

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Fake Admin' }));
    harness.detectChanges();
    fireEvent.click(await screen.findByRole('menuitem', { name: 'Log out' }));

    // Assert
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/login'));
    expect(fake.requests.some((request) => request.method === 'POST')).toBe(true);
  });

  it('sends a signed-in user away from the login page', async () => {
    // Act
    await start('/login', 'Manager');

    // Assert
    expect(TestBed.inject(Router).url).toBe('/');
  });
});
