import { TestBed } from '@angular/core/testing';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import {
  SESSION_EXPIRED_HANDLER,
  STAFF_API,
  STAFF_API_FETCH,
  sessionExpiryMiddleware,
} from './staff-api';

describe('STAFF_API', () => {
  let fake: FakeFetch;
  let sessionExpired: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fake = new FakeFetch();
    sessionExpired = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        { provide: STAFF_API_FETCH, useValue: fake.fetch },
        { provide: SESSION_EXPIRED_HANDLER, useValue: sessionExpired },
      ],
    });
    document.cookie = 'XSRF-TOKEN=fake%2Btoken; path=/';
  });

  afterEach(() => {
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
  });

  it('copies the XSRF-TOKEN cookie into the header on unsafe requests only', async () => {
    // Arrange
    fake.on('POST', '/api/staff/auth/logout', noContent);
    fake.on('GET', '/api/staff/auth/antiforgery', noContent);
    const api = TestBed.inject(STAFF_API);

    // Act
    await api.POST('/api/staff/auth/logout');
    await api.GET('/api/staff/auth/antiforgery');

    // Assert
    expect(fake.requests[0].headers.get('X-XSRF-TOKEN')).toBe('fake+token');
    expect(fake.requests[1].headers.get('X-XSRF-TOKEN')).toBeNull();
  });

  it('does not report an ended session for a 401 from the auth endpoints', async () => {
    // Arrange
    fake.on('GET', '/api/staff/auth/me', () => json(401, { status: 401 }));
    const api = TestBed.inject(STAFF_API);

    // Act
    await api.GET('/api/staff/auth/me');

    // Assert
    expect(sessionExpired).not.toHaveBeenCalled();
  });
});

describe('sessionExpiryMiddleware', () => {
  async function respond(path: string, status: number): Promise<boolean> {
    const onSessionExpired = vi.fn();
    const middleware = sessionExpiryMiddleware(onSessionExpired);
    await middleware.onResponse?.({
      request: new Request(`http://localhost${path}`),
      response: new Response(null, { status }),
    } as never);
    return onSessionExpired.mock.calls.length > 0;
  }

  it('reports an ended session when another staff endpoint answers 401', async () => {
    expect(await respond('/api/staff/positions', 401)).toBe(true);
  });

  it('ignores other statuses and the auth endpoints', async () => {
    expect(await respond('/api/staff/positions', 403)).toBe(false);
    expect(await respond('/api/staff/auth/logout', 401)).toBe(false);
  });
});
