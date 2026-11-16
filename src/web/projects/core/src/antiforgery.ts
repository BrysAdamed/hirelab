import type { Middleware } from 'openapi-fetch';

/** The readable cookie in which the API sends the staff app's antiforgery request token (ADR 0007). */
export const ANTIFORGERY_COOKIE = 'XSRF-TOKEN';

/**
 * The readable cookie in which the API sends the candidate app's antiforgery request token. It differs from the staff
 * one so the two apps opened in one browser don't overwrite each other's token (ADR 0031).
 */
export const CANDIDATE_ANTIFORGERY_COOKIE = 'XSRF-TOKEN-CANDIDATE';

/** The header the API expects the antiforgery request token in on unsafe methods. */
export const ANTIFORGERY_HEADER = 'X-XSRF-TOKEN';

const safeMethods = new Set(['GET', 'HEAD', 'OPTIONS', 'TRACE']);

/** Reads a cookie by name from `document.cookie`, or returns `null`. */
export function readCookie(document: Document, name: string): string | null {
  const prefix = `${name}=`;
  const cookie = document.cookie
    .split(';')
    .map((part) => part.trim())
    .find((part) => part.startsWith(prefix));
  return cookie ? decodeURIComponent(cookie.slice(prefix.length)) : null;
}

/**
 * An `openapi-fetch` middleware that copies the app's antiforgery cookie (`XSRF-TOKEN` unless told otherwise) into the
 * `X-XSRF-TOKEN` header on every unsafe request, as the API's antiforgery check requires.
 */
export function antiforgeryMiddleware(document: Document, cookieName = ANTIFORGERY_COOKIE): Middleware {
  return {
    onRequest({ request }) {
      if (safeMethods.has(request.method.toUpperCase())) {
        return undefined;
      }
      const token = readCookie(document, cookieName);
      if (token) {
        request.headers.set(ANTIFORGERY_HEADER, token);
      }
      return request;
    },
  };
}
