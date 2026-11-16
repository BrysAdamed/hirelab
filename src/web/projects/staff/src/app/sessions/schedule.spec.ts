import { fromLocalInput, toLocalInput } from './schedule';

describe('schedule', () => {
  it('round-trips a timestamp through the datetime-local value', () => {
    // Arrange
    const timestamp = '2026-02-03T10:30:00.000Z';

    // Act
    const value = toLocalInput(timestamp);

    // Assert
    expect(value).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
    expect(fromLocalInput(value)).toBe(timestamp);
  });

  it('treats an empty or invalid value as no schedule', () => {
    // Act and assert
    expect(toLocalInput(null)).toBe('');
    expect(fromLocalInput('')).toBeNull();
    expect(fromLocalInput('not a date')).toBeNull();
  });
});
