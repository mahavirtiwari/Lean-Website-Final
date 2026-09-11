import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AdminApiService } from '../../../core/services/admin-api.service';
import { UiService } from '../../../core/services/ui.service';
import { ResourceFormComponent } from './resource-form.component';
import { FieldConfig } from './resource.config';

const FIELDS: FieldConfig[] = [
  { name: 'heading', label: 'Heading', type: 'text' },
  { name: 'settingsJson', label: 'Block settings (JSON)', type: 'json' },
];

/** The errors the form raises, so a refused save can be asserted on. */
const errors: string[] = [];

async function create(value: Record<string, unknown>) {
  errors.length = 0;
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      { provide: AdminApiService, useValue: {} },
      { provide: UiService, useValue: { error: (m: string) => errors.push(m), success: () => {} } },
    ],
  });

  const fixture = TestBed.createComponent(ResourceFormComponent);
  fixture.componentRef.setInput('fields', FIELDS);
  fixture.componentRef.setInput('value', value);
  await fixture.whenStable();

  return {
    fixture,
    // These are protected: the template reaches them, and so does this, without
    // widening the component's surface for everyone else.
    form: fixture.componentInstance as unknown as {
      text(name: string): string;
      set(name: string, value: unknown): void;
      jsonError(name: string): string | null;
      formatJson(name: string): void;
      submit(): void;
    },
  };
}

describe('ResourceFormComponent JSON fields', () => {
  it('lays a stored one-line value out over several lines when the form opens', async () => {
    const { form } = await create({ settingsJson: '{"cards":[{"title":"Scheme Guideline"}]}' });

    expect(form.text('settingsJson')).toBe(
      '{\n  "cards": [\n    {\n      "title": "Scheme Guideline"\n    }\n  ]\n}',
    );
  });

  it('leaves a broken value exactly as typed, so it can still be repaired', async () => {
    const broken = '{"cards":[{"title":"Scheme Guideline"},]}';
    const { form } = await create({ settingsJson: broken });

    expect(form.text('settingsJson')).toBe(broken);
    expect(form.jsonError('settingsJson')).not.toBeNull();
  });

  it('treats an empty value as valid, since a block need not carry settings', async () => {
    const { form } = await create({ settingsJson: '' });

    expect(form.jsonError('settingsJson')).toBeNull();
  });

  it('refuses to save settings that do not parse', async () => {
    const { form } = await create({ settingsJson: '{"cards":[]}' });

    form.set('settingsJson', '{"cards":[}');
    form.submit();

    expect(errors.length).toBe(1);
    expect(errors[0]).toContain('Block settings (JSON)');
  });

  it('saves once the settings parse again', async () => {
    const { form } = await create({ settingsJson: '{"cards":[]}' });

    form.set('settingsJson', '{"cards":[}');
    form.submit();
    expect(errors.length).toBe(1);

    form.set('settingsJson', '{"cards":[{"title":"Fixed"}]}');
    form.submit();

    // Still the one complaint from the broken attempt: the second went through.
    expect(errors.length).toBe(1);
  });

  it('re-indents a value on request', async () => {
    const { form } = await create({ settingsJson: '' });

    form.set('settingsJson', '{"a":1,"b":[2,3]}');
    form.formatJson('settingsJson');

    expect(form.text('settingsJson')).toBe('{\n  "a": 1,\n  "b": [\n    2,\n    3\n  ]\n}');
  });
});
