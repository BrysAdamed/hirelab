import type { Provider } from '@angular/core';
import {
  LIVE_HUB_CONNECTION_FACTORY,
  LIVE_HUB_RETRY_DELAYS,
  type LiveHubConnection,
} from '../live';

type Answer = (...args: unknown[]) => unknown;

/**
 * A hub connection for tests: it records what the client invokes and lets the test send messages, drop the
 * connection and bring it back. Provide it with {@link provideFakeLiveHub}.
 */
export class FakeLiveHubConnection implements LiveHubConnection {
  readonly paths: string[] = [];
  readonly invocations: { readonly method: string; readonly args: unknown[] }[] = [];
  starts = 0;
  failStarts = 0;
  stopped = false;
  private readonly handlers = new Map<string, (...args: unknown[]) => void>();
  private readonly answers = new Map<string, Answer>();
  private reconnecting: (() => void) | null = null;
  private reconnected: (() => void) | null = null;

  /** Answers invocations of `method` with what `answer` returns. */
  answer(method: string, answer: Answer): this {
    this.answers.set(method, answer);
    return this;
  }

  /** Delivers a message from the hub. */
  send(method: string, ...args: unknown[]): void {
    this.handlers.get(method)?.(...args);
  }

  /** Loses the connection, as the client sees it before it reconnects. */
  drop(): void {
    this.reconnecting?.();
  }

  /** Reconnects after {@link drop}. */
  restore(): void {
    this.reconnected?.();
  }

  start(): Promise<void> {
    this.starts++;
    this.stopped = false;
    return this.failStarts-- > 0 ? Promise.reject(new Error('Offline')) : Promise.resolve();
  }

  stop(): Promise<void> {
    this.stopped = true;
    return Promise.resolve();
  }

  on(method: string, handler: (...args: unknown[]) => void): void {
    this.handlers.set(method, handler);
  }

  invoke<T>(method: string, ...args: unknown[]): Promise<T> {
    this.invocations.push({ method, args });
    return Promise.resolve(this.answers.get(method)?.(...args) as T);
  }

  onreconnecting(callback: () => void): void {
    this.reconnecting = callback;
  }

  onreconnected(callback: () => void): void {
    this.reconnected = callback;
  }

  onclose(): void {
    return undefined;
  }
}

/** Provides `connection` for every live hub, with no delay between attempts to connect. */
export function provideFakeLiveHub(connection: FakeLiveHubConnection): Provider[] {
  return [
    { provide: LIVE_HUB_RETRY_DELAYS, useValue: [0] },
    {
      provide: LIVE_HUB_CONNECTION_FACTORY,
      useValue: (path: string) => {
        connection.paths.push(path);
        return connection;
      },
    },
  ];
}
