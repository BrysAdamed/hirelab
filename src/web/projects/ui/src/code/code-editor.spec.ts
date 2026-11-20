import { render } from '@testing-library/angular';
import { CodeEditor } from './code-editor';
import { provideFakeMonaco } from './testing/fake-monaco';

describe('CodeEditor', () => {
  async function setup() {
    const fake = provideFakeMonaco();
    const valueChange = vi.fn();
    const view = await render(CodeEditor, {
      providers: fake.providers,
      inputs: { value: 'const fake = 1;', language: 'typescript' },
      on: { valueChange },
    });
    await vi.waitFor(() => expect(fake.monaco.codeEditors).toHaveLength(1));
    await view.fixture.whenStable();
    return { ...fake, view, valueChange, codeEditor: fake.monaco.codeEditors[0] };
  }

  it('edits the value in the given language', async () => {
    // Act
    const { codeEditor } = await setup();

    // Assert
    expect(codeEditor.model?.getValue()).toBe('const fake = 1;');
    expect(codeEditor.model?.language).toBe('typescript');
    expect(codeEditor.createOptions['readOnly']).toBe(false);
  });

  it('emits the value on every edit', async () => {
    // Arrange
    const { codeEditor, valueChange } = await setup();

    // Act
    codeEditor.model?.setValue('const fake = 2;');

    // Assert
    expect(valueChange).toHaveBeenCalledWith('const fake = 2;');
  });

  it('follows input changes without echoing them back', async () => {
    // Arrange
    const { view, codeEditor, valueChange } = await setup();

    // Act
    view.fixture.componentRef.setInput('value', 'SELECT 1;');
    view.fixture.componentRef.setInput('language', 'sql');
    view.fixture.componentRef.setInput('readOnly', true);
    await view.fixture.whenStable();

    // Assert
    expect(codeEditor.model?.getValue()).toBe('SELECT 1;');
    expect(codeEditor.model?.language).toBe('sql');
    expect(codeEditor.options['readOnly']).toBe(true);
    expect(valueChange).not.toHaveBeenCalled();
  });

  it('disposes the editor and model on destroy', async () => {
    // Arrange
    const { view, codeEditor } = await setup();

    // Act
    view.fixture.destroy();

    // Assert
    expect(codeEditor.disposed).toBe(true);
    expect(codeEditor.model?.disposed).toBe(true);
  });
});
