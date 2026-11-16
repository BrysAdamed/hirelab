import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import { STAFF_API_FETCH, type UserSummary } from '../api/staff-api';
import { AuthService } from '../auth/auth.service';
import { FakeFetch, fakeUser, json } from '../testing/fake-fetch';
import { UsersPage } from './users-page';

describe('UsersPage', () => {
  const admin = fakeUser('Admin');
  const users: UserSummary[] = [
    {
      id: admin.id,
      email: admin.email,
      displayName: admin.displayName,
      role: 'Admin',
      team: null,
      isActive: true,
    },
    {
      id: '0190a5c4-7e3d-7b8a-9f21-000000000031',
      email: 'mona@example.com',
      displayName: 'Mona Manager',
      role: 'Manager',
      team: { id: '0190a5c4-7e3d-7b8a-9f21-000000000001', name: 'Fake Team' },
      isActive: true,
    },
  ];

  afterEach(() => TestBed.resetTestingModule());

  it('lists every user with their team and offers no action on the admin’s own row', async () => {
    // Arrange
    const fake = new FakeFetch().on('GET', '/api/staff/users', () => json(200, users));
    await render(UsersPage, { providers: [{ provide: STAFF_API_FETCH, useValue: fake.fetch }] });

    // Act
    TestBed.inject(AuthService).signedIn(admin);

    // Assert
    const managerRow = (await screen.findByText('mona@example.com')).closest('tr')!;
    expect(within(managerRow).getByText('Fake Team')).toBeTruthy();
    expect(
      within(managerRow).getByRole('button', { name: 'Deactivate Mona Manager' }),
    ).toBeTruthy();
    const ownRow = screen.getByText(admin.email).closest('tr')!;
    await vi.waitFor(() => expect(within(ownRow).queryByRole('button')).toBeNull());
  });
});
