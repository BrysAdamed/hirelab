import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen } from '@testing-library/angular';
import { STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { ChangePasswordPage } from './change-password-page';

describe('ChangePasswordPage', () => {
  const path = '/api/staff/auth/change-password';
  let fake: FakeFetch;

  async function renderPage(): Promise<void> {
    await render(ChangePasswordPage, {
      providers: [{ provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
  }

  function change(current: string, next: string, confirmation = next): void {
    fireEvent.input(screen.getByLabelText('Current password'), { target: { value: current } });
    fireEvent.input(screen.getByLabelText('New password'), { target: { value: next } });
    fireEvent.input(screen.getByLabelText('Confirm new password'), {
      target: { value: confirmation },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Change password' }));
  }

  function posted(): Request | undefined {
    return fake.requests.find(
      (request) => request.method === 'POST' && new URL(request.url).pathname === path,
    );
  }

  beforeEach(() => {
    fake = new FakeFetch();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('changes the password', async () => {
    // Arrange
    fake.on('POST', path, noContent);
    await renderPage();

    // Act
    change('old-password-not-a-secret', 'new-password-not-a-secret');

    // Assert
    expect((await screen.findByRole('status')).textContent).toContain('Your password is changed.');
    expect(await posted()?.json()).toEqual({
      currentPassword: 'old-password-not-a-secret',
      newPassword: 'new-password-not-a-secret',
    });
  });

  it('shows the API message for a wrong current password', async () => {
    // Arrange
    fake.on('POST', path, () =>
      json(400, { status: 400, detail: 'The current password is incorrect.' }),
    );
    await renderPage();

    // Act
    change('wrong-password-not-a-secret', 'new-password-not-a-secret');

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain(
      'The current password is incorrect.',
    );
  });

  it('checks the new password before calling the API', async () => {
    // Arrange
    await renderPage();

    // Act
    change('old-password-not-a-secret', 'new-password-not-a-secret', 'other-password-not-a-secret');

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('don’t match');
    expect(posted()).toBeUndefined();
  });
});
