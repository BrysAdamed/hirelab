import { Component } from '@angular/core';
import { TaskTypeRegistry } from './task-type-registry';

@Component({ selector: 'hl-stub-task', template: '' })
class StubTask {}

describe('TaskTypeRegistry', () => {
  const registry = new TaskTypeRegistry<StubTask>({
    'stub-type': { loadComponent: () => Promise.resolve(StubTask) },
  });

  it('loads the component registered for a key', async () => {
    // Act
    const component = await registry.find('stub-type')?.loadComponent();

    // Assert
    expect(component).toBe(StubTask);
  });

  it('finds nothing for an unknown key', () => {
    // Act
    const entry = registry.find('unknown-type');

    // Assert
    expect(entry).toBeUndefined();
  });

  it('lists its keys', () => {
    // Assert
    expect(registry.keys).toEqual(['stub-type']);
  });

  it('refuses keys that are not kebab-case', () => {
    // Act
    const create = () =>
      new TaskTypeRegistry<StubTask>({
        'Stub Type': { loadComponent: () => Promise.resolve(StubTask) },
      });

    // Assert
    expect(create).toThrow(/kebab-case: Stub Type/);
  });
});
