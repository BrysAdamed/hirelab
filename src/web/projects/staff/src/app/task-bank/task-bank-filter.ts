import type { Params } from '@angular/router';
import type { TaskLevel } from '../api/staff-api';

export const taskLevels: readonly TaskLevel[] = ['Junior', 'Mid', 'Senior'];

/**
 * The task bank's filters: a task has every one of `tags`, one of `levels` (any level when empty), the task type
 * `taskType` (any when `null`) and `search` in its title or summary.
 */
export interface TaskBankFilter {
  readonly tags: readonly string[];
  readonly levels: readonly TaskLevel[];
  readonly taskType: string | null;
  readonly search: string;
}

/** The task bank page's query parameters, which hold its filter so a link or the back button restores it. */
export interface TaskBankQuery {
  readonly tag?: string | readonly string[];
  readonly level?: string | readonly string[];
  readonly type?: string;
  readonly search?: string;
}

export const noFilter: TaskBankFilter = { tags: [], levels: [], taskType: null, search: '' };

export function isFiltered(filter: TaskBankFilter): boolean {
  return (
    filter.tags.length > 0 ||
    filter.levels.length > 0 ||
    filter.taskType !== null ||
    filter.search.length > 0
  );
}

export function filterFromQuery(query: TaskBankQuery): TaskBankFilter {
  const levels = valuesOf(query.level);
  return {
    tags: valuesOf(query.tag),
    levels: taskLevels.filter((level) => levels.includes(level)),
    taskType: query.type?.trim() || null,
    search: query.search?.trim() ?? '',
  };
}

/** The query parameters for `filter`; an unset filter is `null`, which removes its parameter. */
export function queryFromFilter(filter: TaskBankFilter): Params {
  return {
    tag: filter.tags.length > 0 ? [...filter.tags] : null,
    level: filter.levels.length > 0 ? [...filter.levels] : null,
    type: filter.taskType,
    search: filter.search || null,
  };
}

function valuesOf(value: string | readonly string[] | undefined): string[] {
  const values = value === undefined ? [] : typeof value === 'string' ? [value] : value;
  return [...new Set(values.map((item) => item.trim()).filter((item) => item.length > 0))];
}
