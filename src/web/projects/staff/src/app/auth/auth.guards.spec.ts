import { safeReturnUrl } from './auth.guards';

describe('safeReturnUrl', () => {
  it.each([
    ['/positions?tab=open', '/positions?tab=open'],
    [null, '/'],
    ['', '/'],
    ['https://evil.example.com/', '/'],
    ['//evil.example.com/', '/'],
    ['/login?returnUrl=%2F', '/'],
  ])('turns %s into %s', (returnUrl, expected) => {
    expect(safeReturnUrl(returnUrl)).toBe(expected);
  });
});
