import { Injectable, inject } from '@angular/core';
import { type ApiResult, apiFailure } from '../api/api-result';
import { type BankTask, STAFF_API, type TaskSummary } from '../api/staff-api';
import type { TaskBankFilter } from './task-bank-filter';

/** The task bank endpoints. */
@Injectable({ providedIn: 'root' })
export class TaskBankService {
  private readonly api = inject(STAFF_API);

  async list(filter: TaskBankFilter): Promise<ApiResult<TaskSummary[]>> {
    const { data, error } = await this.api.GET('/api/staff/tasks', {
      params: {
        query: {
          tag: filter.tags.length > 0 ? [...filter.tags] : undefined,
          level: filter.levels.length > 0 ? [...filter.levels] : undefined,
          taskType: filter.taskType ?? undefined,
          search: filter.search || undefined,
        },
      },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async get(taskId: string): Promise<ApiResult<BankTask>> {
    const { data, error } = await this.api.GET('/api/staff/tasks/{taskId}', {
      params: { path: { taskId } },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async tags(): Promise<ApiResult<string[]>> {
    const { data, error } = await this.api.GET('/api/staff/tasks/tags');
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  async taskTypes(): Promise<ApiResult<string[]>> {
    const { data, error } = await this.api.GET('/api/staff/tasks/types');
    return data ? { ok: true, value: data } : apiFailure(error);
  }
}
