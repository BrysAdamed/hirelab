import { render, screen } from '@testing-library/angular';
import { PresenceIndicator } from './presence-indicator';
import { ReconnectingBanner } from './reconnecting-banner';

describe('PresenceIndicator', () => {
  it('shows an online candidate with more than one connection', async () => {
    // Arrange & Act
    await render(PresenceIndicator, {
      inputs: { presence: { connected: true, connections: 2, lastSeenAt: '2026-01-05T09:00:00Z' } },
    });

    // Assert
    expect(screen.getByText(/Candidate online/).textContent).toContain('2 connections');
  });

  it('shows an online candidate with one connection without a count', async () => {
    // Arrange & Act
    await render(PresenceIndicator, {
      inputs: { presence: { connected: true, connections: 1, lastSeenAt: '2026-01-05T09:00:00Z' } },
    });

    // Assert
    expect(screen.getByText(/Candidate online/).textContent).not.toContain('connections');
  });

  it('shows when an offline candidate was last seen', async () => {
    // Arrange & Act
    await render(PresenceIndicator, {
      inputs: {
        presence: { connected: false, connections: 0, lastSeenAt: '2026-01-05T09:00:00Z' },
      },
    });

    // Assert
    expect(screen.getByText(/Candidate offline · last seen/)).toBeTruthy();
  });

  it('says when the candidate has not connected yet', async () => {
    // Arrange & Act
    await render(PresenceIndicator, {
      inputs: { presence: { connected: false, connections: 0, lastSeenAt: null } },
    });

    // Assert
    expect(screen.getByText(/Candidate not connected yet/)).toBeTruthy();
  });
});

describe('ReconnectingBanner', () => {
  it('announces reconnecting in a live region', async () => {
    // Arrange & Act
    const { container } = await render(ReconnectingBanner, { inputs: { reconnecting: true } });

    // Assert
    expect(container.querySelector('[aria-live="polite"]')?.textContent).toContain(
      'Reconnecting… Live updates are paused.',
    );
  });

  it('keeps the live region empty while connected', async () => {
    // Arrange & Act
    const { container } = await render(ReconnectingBanner, { inputs: { reconnecting: false } });

    // Assert
    expect(container.querySelector('[aria-live="polite"]')?.textContent?.trim()).toBe('');
  });
});
