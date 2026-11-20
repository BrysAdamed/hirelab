import { Component, input, output } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TaskTypeRegistry, type TaskContent } from '@hirelab/tasks/framework';
import type {
  AnyEditorTaskComponent,
  EditorTaskComponent,
  TaskContentErrors,
} from './editor-task-component';
import { EDITOR_TASK_TYPES, editorTaskTypes } from './editor-task-types';

interface StubContent {
  readonly phrase: string;
}

interface StubStaffContent {
  readonly matchCriterionId: string;
}

@Component({ selector: 'hl-stub-editor-task', template: '' })
class StubEditorTask implements EditorTaskComponent<StubContent, StubStaffContent> {
  readonly content = input.required<TaskContent<StubContent, StubStaffContent>>();
  readonly errors = input<TaskContentErrors>({});
  readonly contentChange = output<TaskContent<StubContent, StubStaffContent>>();
}

describe('editor task types', () => {
  it('ship the open question and code review types', () => {
    // Assert
    expect(editorTaskTypes.keys).toEqual(['open-question', 'code-review']);
  });

  it('are injected from the shipped registry', () => {
    // Act
    const registry = TestBed.inject(EDITOR_TASK_TYPES);

    // Assert
    expect(registry).toBe(editorTaskTypes);
  });

  it('accept a component that implements the editor contract', async () => {
    // Arrange
    const registry = new TaskTypeRegistry<AnyEditorTaskComponent>({
      'stub-type': { loadComponent: () => Promise.resolve(StubEditorTask) },
    });
    TestBed.configureTestingModule({
      providers: [{ provide: EDITOR_TASK_TYPES, useValue: registry }],
    });

    // Act
    const component = await TestBed.inject(EDITOR_TASK_TYPES).find('stub-type')?.loadComponent();

    // Assert
    expect(component).toBe(StubEditorTask);
  });
});
