import type { Type } from '@angular/core';

/** A task type's lazily loaded UI for one entry point (`candidate`, `review` or `editor`). */
export interface TaskTypeEntry<TComponent> {
  readonly loadComponent: () => Promise<Type<TComponent>>;
}

const taskTypeKey = /^[a-z][a-z0-9]*(-[a-z0-9]+)*$/;

/**
 * Maps a task type key, the same kebab-case key as the backend's `ITaskType.Key`, to the lazily loaded
 * component of one entry point (ADR 0013, ADR 0028).
 */
export class TaskTypeRegistry<TComponent> {
  private readonly entries: ReadonlyMap<string, TaskTypeEntry<TComponent>>;

  constructor(entries: Readonly<Record<string, TaskTypeEntry<TComponent>>>) {
    const invalid = Object.keys(entries).filter((key) => !taskTypeKey.test(key));
    if (invalid.length > 0) {
      throw new Error(`Task type keys must be kebab-case: ${invalid.join(', ')}.`);
    }
    this.entries = new Map(Object.entries(entries));
  }

  /** Every registered key, in registration order. */
  get keys(): readonly string[] {
    return [...this.entries.keys()];
  }

  /** The entry for `key`, or `undefined` when this app has no UI for that task type. */
  find(key: string): TaskTypeEntry<TComponent> | undefined {
    return this.entries.get(key);
  }
}
