import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen } from '@testing-library/angular';
import { STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json } from '../testing/fake-fetch';
import { CandidateLinkAction } from './candidate-link-action';

describe('CandidateLinkAction', () => {
  const sessionId = '0190a5c4-7e3d-7b8a-9f21-000000000081';
  const path = `/api/staff/sessions/${sessionId}/candidate-link`;

  let fake: FakeFetch;

  async function renderAction(linkCreatedAt: string | null = null) {
    fake = new FakeFetch();
    const issued = vi.fn();
    await render(CandidateLinkAction, {
      inputs: { sessionId, sessionNumber: 2, linkCreatedAt },
      on: { issued },
      providers: [{ provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
    return issued;
  }

  afterEach(() => TestBed.resetTestingModule());

  it('creates the link and shows it once with a copy button', async () => {
    // Arrange
    const issued = await renderAction();
    fake.on('POST', path, () =>
      json(200, { linkPath: '/s/fake-link-token', createdAt: '2026-01-07T10:00:00Z' }),
    );

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Create candidate link' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Create link' }));

    // Assert
    const link = (await screen.findByRole('textbox', {
      name: 'Candidate link',
    })) as HTMLInputElement;
    expect(link.value).toBe(`${location.origin}/s/fake-link-token`);
    expect(screen.getByRole('button', { name: 'Copy link' })).toBeTruthy();
    expect(screen.getByText(/isn’t shown again/)).toBeTruthy();

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Done' }));

    // Assert
    await vi.waitFor(() => expect(issued).toHaveBeenCalledOnce());
  });

  it('warns that regenerating stops the old link', async () => {
    // Arrange
    await renderAction('2026-01-07T10:00:00Z');

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Regenerate link' }));

    // Assert
    expect(
      await screen.findByRole('heading', { name: 'Regenerate the candidate link' }),
    ).toBeTruthy();
    expect(screen.getByText(/old link stops working/)).toBeTruthy();
  });

  it('shows why the link was refused and emits nothing', async () => {
    // Arrange
    const issued = await renderAction();
    fake.on('POST', path, () =>
      json(409, {
        status: 409,
        detail: 'This candidate has been anonymised, so they can’t be interviewed any more.',
        code: 'InterviewSession.CandidateAnonymised',
      }),
    );

    // Act
    fireEvent.click(screen.getByRole('button', { name: 'Create candidate link' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Create link' }));

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('anonymised');
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await new Promise((resolve) => setTimeout(resolve));
    expect(issued).not.toHaveBeenCalled();
  });
});
