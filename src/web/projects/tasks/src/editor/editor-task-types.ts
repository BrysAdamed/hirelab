import { InjectionToken } from '@angular/core';
import { TaskTypeRegistry } from '@hirelab/tasks/framework';
import type { AnyEditorTaskComponent } from './editor-task-component';

/** The `editor` entry point of every task type. Staff only. */
export const editorTaskTypes = new TaskTypeRegistry<AnyEditorTaskComponent>({});

export const EDITOR_TASK_TYPES = new InjectionToken<TaskTypeRegistry<AnyEditorTaskComponent>>(
  'EDITOR_TASK_TYPES',
  { providedIn: 'root', factory: () => editorTaskTypes },
);
