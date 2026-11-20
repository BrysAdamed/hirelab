import { createEmptyCodeReviewResponse } from './code-review';

describe('createEmptyCodeReviewResponse', () => {
  it('has no comments, an empty summary and no verdict', () => {
    expect(createEmptyCodeReviewResponse()).toEqual({ comments: [], summary: '' });
  });

  it('returns a new response each time', () => {
    expect(createEmptyCodeReviewResponse()).not.toBe(createEmptyCodeReviewResponse());
  });
});
