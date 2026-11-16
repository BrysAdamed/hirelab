import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen } from '@testing-library/angular';
import { STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json } from '../testing/fake-fetch';
import { ResetLinkAction } from './reset-link-action';
import type { ResetLinkKind } from './reset-links.service';

describe('ResetLinkAction', () => {
  const userId = '0190a5c4-7e3d-7b8a-9f21-00000000000b';
  let fake: FakeFetch;

  async function renderAction(kind: ResetLinkKind): Promise<void> {
    await render(ResetLinkAction, {
      inputs: { userId, displayName: 'Ivy Interviewer', kind },
      providers: [{ provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
  }

  function posted(path: string): Request | undefined {
    return fake.requests.find(
      (request) => request.method === 'POST' && new URL(request.url).pathname === path,
    );
  }

  beforeEach(() => {
    fake = new FakeFetch();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('generates a link after confirmation and shows it once', async () => {
    // Arrange
    fake.on('POST', `/api/staff/reset-links/interviewers/${userId}`, () =>
      json(200, { userId, expiresAt: '2026-01-06T09:00:00+00:00', linkPath: '/reset#fake-token' }),
    );
    await renderAction('interviewers');

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Reset the password of Ivy Interviewer' }));
    const generatedBefore = posted(`/api/staff/reset-links/interviewers/${userId}`) !== undefined;
    fireEvent.click(await screen.findByRole('button', { name: 'Generate link' }));

    // Assert
    const link = (await screen.findByLabelText('Reset link')) as HTMLInputElement;
    expect(link.value).toBe(`${document.location.origin}/reset#fake-token`);
    expect(generatedBefore).toBe(false);
  });

  it('uses the managers endpoint for the admin', async () => {
    // Arrange
    fake.on('POST', `/api/staff/reset-links/managers/${userId}`, () =>
      json(200, { userId, expiresAt: '2026-01-06T09:00:00+00:00', linkPath: '/reset#fake-token' }),
    );
    await renderAction('managers');

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Reset the password of Ivy Interviewer' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Generate link' }));

    // Assert
    await screen.findByLabelText('Reset link');
    expect(posted(`/api/staff/reset-links/managers/${userId}`)).toBeDefined();
  });

  it('shows the API message when no link can be generated', async () => {
    // Arrange
    fake.on('POST', `/api/staff/reset-links/interviewers/${userId}`, () =>
      json(409, { status: 409, detail: 'This user is deactivated.' }),
    );
    await renderAction('interviewers');

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Reset the password of Ivy Interviewer' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Generate link' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('This user is deactivated.');
    expect(screen.queryByLabelText('Reset link')).toBeNull();
  });
});
