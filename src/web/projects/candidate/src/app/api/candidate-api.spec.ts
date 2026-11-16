import { TestBed } from '@angular/core/testing';
import { FakeFetch, noContent } from '../testing/fake-fetch';
import { CANDIDATE_API, CANDIDATE_API_FETCH } from './candidate-api';

describe('CANDIDATE_API', () => {
  let fake: FakeFetch;

  beforeEach(() => {
    fake = new FakeFetch();
    TestBed.configureTestingModule({
      providers: [{ provide: CANDIDATE_API_FETCH, useValue: fake.fetch }],
    });
    document.cookie = 'XSRF-TOKEN-CANDIDATE=fake%2Bcandidate%2Btoken; path=/';
    document.cookie = 'XSRF-TOKEN=fake%2Bstaff%2Btoken; path=/';
  });

  afterEach(() => {
    document.cookie = 'XSRF-TOKEN-CANDIDATE=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/';
    TestBed.resetTestingModule();
  });

  it('copies the candidate antiforgery cookie, not the staff one, into the header on unsafe requests only', async () => {
    // Arrange
    fake
      .on('GET', '/api/candidate/access/antiforgery', noContent)
      .on('POST', '/api/candidate/access', noContent);
    const api = TestBed.inject(CANDIDATE_API);

    // Act
    await api.GET('/api/candidate/access/antiforgery');
    await api.POST('/api/candidate/access', { body: { token: 'fake-link-token' } });

    // Assert
    const [get, post] = fake.requests;
    expect(get.headers.get('X-XSRF-TOKEN')).toBeNull();
    expect(post.headers.get('X-XSRF-TOKEN')).toBe('fake+candidate+token');
  });
});
