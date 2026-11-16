import { InjectionToken, inject, signal } from '@angular/core';
import {
  HubConnectionBuilder,
  type HubConnection,
  type IRetryPolicy,
  type RetryContext,
} from '@microsoft/signalr';

/**
 * Where a live hub connection stands: opening for the first time (`connecting`), `connected`, or lost and trying
 * again (`reconnecting`), which the apps show as "Reconnecting…".
 */
export type LiveHubStatus = 'connecting' | 'connected' | 'reconnecting';

/** A hub's methods as the generated contract describes them: their arguments and what they return. */
export type HubMethods<TMethods> = {
  [TMethod in keyof TMethods]: { args: unknown[]; returns: unknown };
};

/** A hub's messages as the generated contract describes them, with their arguments. */
export type HubEvents<TEvents> = { [TEvent in keyof TEvents]: unknown[] };

/** What to do with each message of a hub. */
export type HubEventHandlers<TEvents extends HubEvents<TEvents>> = {
  readonly [TEvent in keyof TEvents]?: (...args: TEvents[TEvent]) => void;
};

/** The parts of a SignalR connection {@link LiveHub} uses, so tests can stand in for it. */
export type LiveHubConnection = Pick<
  HubConnection,
  'start' | 'stop' | 'on' | 'invoke' | 'onreconnecting' | 'onreconnected' | 'onclose'
>;

/** How long to wait before each attempt to connect again, in milliseconds; the last delay repeats forever. */
export const LIVE_HUB_RETRY_DELAYS = new InjectionToken<readonly number[]>(
  'LIVE_HUB_RETRY_DELAYS',
  {
    providedIn: 'root',
    factory: () => [0, 2_000, 5_000, 10_000, 30_000],
  },
);

/** Opens a SignalR connection to a hub path on the app's own origin. */
export const LIVE_HUB_CONNECTION_FACTORY = new InjectionToken<
  (path: string, retryDelays: readonly number[]) => LiveHubConnection
>('LIVE_HUB_CONNECTION_FACTORY', {
  providedIn: 'root',
  factory: () => (path, retryDelays) =>
    new HubConnectionBuilder()
      .withUrl(path)
      .withAutomaticReconnect(retryPolicy(retryDelays))
      .build(),
});

export interface LiveHubOptions<TEvents extends HubEvents<TEvents>> {
  /** The names of the hub's messages, from the generated contract. */
  readonly events: readonly (keyof TEvents & string)[];
  readonly handlers: HubEventHandlers<TEvents>;
  /**
   * Runs once the connection is open and again after every reconnect. Messages sent while it was down are lost, so
   * this subscribes again and refetches over HTTP (ADR 0006).
   */
  readonly connected: () => void | Promise<void>;
  /** Runs when the connection is lost, before it tries again. */
  readonly reconnecting?: () => void;
}

/**
 * A push-only connection to one of the API's hubs (ADR 0006), typed by the hub contract generated in
 * `@hirelab/api-client`. It reconnects with backoff for as long as it isn't stopped, including when the first attempt
 * fails, and reports where it stands in {@link status}.
 */
export class LiveHub<TMethods extends HubMethods<TMethods>, TEvents extends HubEvents<TEvents>> {
  private readonly statusSignal = signal<LiveHubStatus>('connecting');
  private stopped = false;
  private retryTimer: ReturnType<typeof setTimeout> | undefined;
  private attempts = 0;

  readonly status = this.statusSignal.asReadonly();

  constructor(
    private readonly connection: LiveHubConnection,
    private readonly retryDelays: readonly number[],
    private readonly options: LiveHubOptions<TEvents>,
  ) {
    for (const event of options.events) {
      connection.on(event, (...args: unknown[]) =>
        options.handlers[event]?.(...(args as TEvents[typeof event])),
      );
    }
    connection.onreconnecting(() => {
      this.statusSignal.set('reconnecting');
      options.reconnecting?.();
    });
    connection.onreconnected(() => void this.opened());
    connection.onclose(() => {
      if (!this.stopped) {
        this.statusSignal.set('reconnecting');
        options.reconnecting?.();
        this.retry();
      }
    });
  }

  /** Opens the connection. */
  start(): void {
    this.stopped = false;
    void this.connect();
  }

  /** Calls one of the hub's methods. */
  invoke<TMethod extends keyof TMethods & string>(
    method: TMethod,
    ...args: TMethods[TMethod]['args']
  ): Promise<TMethods[TMethod]['returns']> {
    return this.connection.invoke(method, ...args);
  }

  /** Closes the connection for good. */
  async stop(): Promise<void> {
    this.stopped = true;
    clearTimeout(this.retryTimer);
    await this.connection.stop();
  }

  private async connect(): Promise<void> {
    try {
      await this.connection.start();
    } catch {
      if (!this.stopped) {
        this.statusSignal.set(this.attempts === 0 ? 'connecting' : 'reconnecting');
        this.retry();
      }
      return;
    }
    if (this.stopped) {
      await this.connection.stop();
      return;
    }
    await this.opened();
  }

  private async opened(): Promise<void> {
    this.attempts = 0;
    this.statusSignal.set('connected');
    try {
      await this.options.connected();
    } catch {
      return;
    }
  }

  private retry(): void {
    const delay = this.retryDelays[Math.min(this.attempts, this.retryDelays.length - 1)];
    this.attempts++;
    clearTimeout(this.retryTimer);
    this.retryTimer = setTimeout(() => void this.connect(), delay);
  }
}

function retryPolicy(delays: readonly number[]): IRetryPolicy {
  return {
    nextRetryDelayInMilliseconds: ({ previousRetryCount }: RetryContext) =>
      delays[Math.min(previousRetryCount, delays.length - 1)],
  };
}

/**
 * Creates a {@link LiveHub} for the hub at `path` with the app's connection factory and retry delays. Call it in an
 * injection context, then {@link LiveHub.start} it.
 */
export function injectLiveHub<
  TMethods extends HubMethods<TMethods>,
  TEvents extends HubEvents<TEvents>,
>(path: string, options: LiveHubOptions<TEvents>): LiveHub<TMethods, TEvents> {
  const retryDelays = inject(LIVE_HUB_RETRY_DELAYS);
  const connection = inject(LIVE_HUB_CONNECTION_FACTORY)(path, retryDelays);
  return new LiveHub<TMethods, TEvents>(connection, retryDelays, options);
}
