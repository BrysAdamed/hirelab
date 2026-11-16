type Handler = (request: Request) => Response | Promise<Response>;

/** A `fetch` for tests that answers `METHOD /path` from a table and records every request. */
export class FakeFetch {
  readonly requests: Request[] = [];
  private readonly handlers = new Map<string, Handler>();

  on(method: string, path: string, handler: Handler): this {
    this.handlers.set(`${method} ${path}`, handler);
    return this;
  }

  readonly fetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const request = input instanceof Request ? input : new Request(input, init);
    this.requests.push(request);
    const handler = this.handlers.get(`${request.method} ${new URL(request.url).pathname}`);
    return handler ? handler(request) : json(404, { status: 404 });
  };
}

export function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' },
  });
}

export function noContent(): Response {
  return new Response(null, { status: 204 });
}
