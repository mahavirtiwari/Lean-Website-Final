import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { ContactComponent } from './contact.component';

/**
 * The hosted-form hook, which is how the enquiry form moves to Zoho later.
 *
 * The address it returns goes into an iframe src, so the guard around it is the
 * point of these: a setting that could frame anything at all would be a way to
 * put someone else's page inside a government one.
 */
const AGENCIES = [
  { id: 1, name: 'Quality Council of India', shortName: 'QCI' },
  { id: 2, name: 'National Productivity Council', shortName: 'NPC' },
] as unknown as Parameters<typeof of>[0][];

/** QCI's grievance matrix, cut down to the shape that matters to the form. */
const MATRIX = {
  agency: 'QCI',
  labels: ['User type', 'Complaint or query', 'Related to', 'Specific issue'],
  options: [
    {
      name: 'MSME',
      children: [
        { name: 'Complaint', children: [{ name: 'Assessor', children: [{ name: 'Ethical Issue' }] }] },
        { name: 'Query', children: [{ name: 'LEAN', children: [{ name: 'Payment Related' }, { name: 'Others' }] }] },
      ],
    },
  ],
};

async function withFormUrl(
  formEmbedUrl: string,
  agencies: unknown[] = [],
  grievance: unknown[] = [],
  choice: { askAgency?: boolean; defaultAgency?: string | null } = {},
) {
  // Filled by the test double's submitEnquiry, so a test can see what was sent.
  let submitted: FormData | null = null;
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([]),
      {
        provide: ContentService,
        useValue: {
          settings: () => ({}),
          setting: (key: string, fallback = '') =>
            key === 'contact.formEmbedUrl' ? formEmbedUrl : fallback,
          settingList: (_key: string, fallback: string[]) => fallback,
          getPartners: () => of(agencies),
          // The CMS banner inside the page asks for its own copy.
          getPage: () => of(null),
          getCaptcha: () => of({ id: 'test-challenge', svg: '<svg></svg>' }),
          flag: (_key: string, fallback: boolean) => fallback,
          submitEnquiry: (payload: FormData) => {
            submitted = payload;
            return of({});
          },
          getContactOptions: () => of({ grievance, ...choice }),
        },
      },
      { provide: UiService, useValue: { setMeta: () => {}, success: () => {}, error: () => {} } },
    ],
  });

  const fixture = TestBed.createComponent(ContactComponent);
  await fixture.whenStable();

  return {
    fixture,
    embedded: (fixture.componentInstance as unknown as { embeddedForm(): unknown }).embeddedForm(),
    form: (fixture.componentInstance as unknown as {
      form: {
        value: Record<string, unknown>;
        valid: boolean;
        controls: Record<string, { setValue(v: unknown): void; valid: boolean; hasError(e: string): boolean }>;
        patchValue(v: Record<string, unknown>): void;
      };
    }).form,
    hasIframe: !!(fixture.nativeElement as HTMLElement).querySelector('iframe.contact-embed'),
    // The <form> element itself always exists now - it wraps the agency chooser,
    // which stays on screen so a visitor can pick the other agency. What matters is
    // whether the portal's own fields were built to collect anything.
    hasOwnForm: !!(fixture.nativeElement as HTMLElement).querySelector('[formControlName="name"]'),
    submitted: () => submitted,
  };
}

describe('ContactComponent agency choice', () => {
  it('submits the agency the sender picked, not the browser default', async () => {
    // The DOM value of a radio bound through a form stays "on"; what matters is
    // the value the control receives, which is what would reach the server.
    const { fixture, form } = await withFormUrl('', AGENCIES);
    const el = fixture.nativeElement as HTMLElement;

    const [qci, npc] = [...el.querySelectorAll<HTMLInputElement>('.agency-option input')];

    qci.click();
    await fixture.whenStable();
    expect(form.value['agency']).toBe('QCI');

    npc.click();
    await fixture.whenStable();
    expect(form.value['agency']).toBe('NPC');
  });
});

describe('ContactComponent required fields', () => {
  it('offers only the implementing agencies, with none chosen for the sender', async () => {
    const { fixture, form } = await withFormUrl('', AGENCIES);
    const el = fixture.nativeElement as HTMLElement;

    const labels = [...el.querySelectorAll('.agency-option')].map((l) =>
      (l.textContent ?? '').replace(/\s+/g, ' ').trim(),
    );

    expect(labels.length).toBe(2);
    expect(labels.some((l) => l.includes('Ministry'))).toBe(false);
    expect(form.value['agency']).toBe('');
  });

  it('will not submit without an agency', async () => {
    const { fixture, form } = await withFormUrl('', AGENCIES);

    form.patchValue({
      name: 'A Sender',
      email: 'sender@example.com',
      phone: '9876543210',
      subject: 'A subject',
      message: 'A message that is comfortably longer than the minimum length allowed.',
      captchaAnswer: 'ABCDE',
    });

    expect(form.valid).toBe(false);

    form.patchValue({ agency: 'QCI' });
    expect(form.valid).toBe(true);
  });

  it('requires a mobile number and refuses a landline', async () => {
    const { form } = await withFormUrl('', AGENCIES);
    const phone = form.controls['phone'];

    phone.setValue('');
    expect(phone.hasError('required')).toBe(true);

    // A landline: the team calls these back, and this one could not be returned.
    phone.setValue('011-2306 3800');
    expect(phone.valid).toBe(false);

    phone.setValue('9876543210');
    expect(phone.valid).toBe(true);

    phone.setValue('+91 9876543210');
    expect(phone.valid).toBe(true);

    // Indian mobile numbers start 6 to 9.
    phone.setValue('1234567890');
    expect(phone.valid).toBe(false);
  });

  it('no longer asks for a state', async () => {
    const { fixture, form } = await withFormUrl('', AGENCIES);

    expect('state' in form.value).toBe(false);
    expect((fixture.nativeElement as HTMLElement).querySelector('#contact-state')).toBeNull();
  });
});

describe('ContactComponent hosted form', () => {
  it('keeps the portal form when no address is configured', async () => {
    const { embedded, hasIframe, hasOwnForm } = await withFormUrl('');

    expect(embedded).toBeNull();
    expect(hasIframe).toBe(false);
    expect(hasOwnForm).toBe(true);
  });

  it('shows a hosted form in place of its own when one is configured', async () => {
    const { embedded, hasIframe, hasOwnForm } = await withFormUrl(
      'https://forms.zohopublic.in/example/form/Enquiry/formperma/abc123',
    );

    expect(embedded).not.toBeNull();
    expect(hasIframe).toBe(true);
    // The portal's own form is not merely hidden: it is not built at all, so it
    // cannot collect anything alongside the hosted one.
    expect(hasOwnForm).toBe(false);
  });

  it("prefers the chosen agency's own form over the site-wide one", async () => {
    const agencies = [
      { id: 1, name: 'Quality Council of India', shortName: 'QCI', enquiryFormUrl: 'https://forms.zohopublic.in/qci' },
      { id: 2, name: 'National Productivity Council', shortName: 'NPC' },
    ];

    const { fixture, form } = await withFormUrl('https://forms.zohopublic.in/site-wide', agencies);
    const instance = fixture.componentInstance as unknown as {
      embeddedForm(): { toString(): string } | null;
    };

    form.controls['agency'].setValue('QCI');
    await fixture.whenStable();
    expect(String(instance.embeddedForm())).toContain('/qci');

    // NPC hosts nothing, so the site-wide address applies again rather than the
    // other agency's form being left on screen.
    form.controls['agency'].setValue('NPC');
    await fixture.whenStable();
    expect(String(instance.embeddedForm())).toContain('site-wide');
  });

  it('keeps the agency chooser on screen while a hosted form is shown', async () => {
    const agencies = [
      { id: 1, name: 'Quality Council of India', shortName: 'QCI', enquiryFormUrl: 'https://forms.zohopublic.in/qci' },
      { id: 2, name: 'National Productivity Council', shortName: 'NPC' },
    ];

    const { fixture, form } = await withFormUrl('', agencies);
    form.controls['agency'].setValue('QCI');
    await fixture.whenStable();

    const dom = fixture.nativeElement as HTMLElement;
    expect(dom.querySelector('iframe.contact-embed')).not.toBeNull();
    // Without these a visitor who picked the agency with a hosted form could not
    // pick the other one without reloading the page.
    expect(dom.querySelectorAll('input[type="radio"]').length).toBe(2);
  });

  it('refuses an address that is not https', async () => {
    const { embedded, hasOwnForm } = await withFormUrl('http://forms.example.com/enquiry');

    expect(embedded).toBeNull();
    expect(hasOwnForm).toBe(true);
  });

  it('refuses a script address', async () => {
    // eslint-disable-next-line no-script-url -- the point of the test.
    const { embedded, hasOwnForm } = await withFormUrl('javascript:alert(1)');

    expect(embedded).toBeNull();
    expect(hasOwnForm).toBe(true);
  });

  it('refuses something that is not an address at all', async () => {
    const { embedded, hasOwnForm } = await withFormUrl('paste the zoho link here');

    expect(embedded).toBeNull();
    expect(hasOwnForm).toBe(true);
  });
});

describe('ContactComponent grievance matrix', () => {
  const VALID = {
    name: 'A Sender',
    email: 'sender@example.com',
    phone: '9876543210',
    subject: 'A subject',
    message: 'A message that is comfortably longer than the minimum length allowed.',
    captchaAnswer: 'ABCDE',
  };

  const lists = (el: HTMLElement) =>
    [...el.querySelectorAll<HTMLSelectElement>('fieldset.grievance select')].map((s) => ({
      label: s.closest('.field')?.querySelector('label')?.textContent?.trim(),
      value: s.value,
    }));

  it('shows QCI its linked lists, starting with the only user type chosen', async () => {
    const { fixture, form } = await withFormUrl('', AGENCIES, [MATRIX]);

    form.controls['agency'].setValue('QCI');
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(lists(el)).toEqual([
      { label: 'User type', value: 'MSME' },
      { label: 'Complaint or query', value: '' },
    ]);
    // In place of the ordinary category list, not as well as it.
    expect(el.querySelector('#contact-category')).toBeNull();
  });

  it('gives NPC the ordinary category list and no grievance lists', async () => {
    const { fixture, form } = await withFormUrl('', AGENCIES, [MATRIX]);

    form.controls['agency'].setValue('NPC');
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('fieldset.grievance')).toBeNull();
    expect(el.querySelector('#contact-category')).not.toBeNull();
  });

  it('opens each list from the one before, and clears what no longer applies', async () => {
    const { fixture, form } = await withFormUrl('', AGENCIES, [MATRIX]);
    const el = fixture.nativeElement as HTMLElement;

    // One step at a time, as a visitor would: the second list only appears once
    // the first has its answer.
    form.controls['agency'].setValue('QCI');
    await fixture.whenStable();
    form.controls['issueType'].setValue('Query');
    await fixture.whenStable();

    // "Related to" has one option under Query, so it is chosen for the sender.
    expect(lists(el).map((l) => l.value)).toEqual(['MSME', 'Query', 'LEAN', '']);

    form.controls['issueType'].setValue('Complaint');
    await fixture.whenStable();
    // LEAN belonged to Query and is gone; Assessor, the only choice under
    // Complaint, is chosen, and so is the single issue under it.
    expect(lists(el).map((l) => l.value)).toEqual(['MSME', 'Complaint', 'Assessor', 'Ethical Issue']);
  });

  it('will not send a QCI enquiry until the last list is answered, then sends all four', async () => {
    const { fixture, form, submitted } = await withFormUrl('', AGENCIES, [MATRIX]);
    const component = fixture.componentInstance as unknown as { submit(): void };

    form.patchValue({ ...VALID, agency: 'QCI' });
    await fixture.whenStable();
    form.controls['issueType'].setValue('Query');
    await fixture.whenStable();

    component.submit();
    expect(submitted()).toBeNull();

    form.controls['issueSubCategory'].setValue('Payment Related');
    await fixture.whenStable();
    component.submit();

    const sent = submitted();
    expect(sent).not.toBeNull();
    expect([sent!.get('userType'), sent!.get('issueType'), sent!.get('issueCategory'), sent!.get('issueSubCategory')])
      .toEqual(['MSME', 'Query', 'LEAN', 'Payment Related']);
  });
});

describe('ContactComponent agency question switched off', () => {
  const VALID = {
    name: 'A Sender',
    email: 'sender@example.com',
    phone: '9876543210',
    subject: 'A subject',
    message: 'A message that is comfortably longer than the minimum length allowed.',
    captchaAnswer: 'ABCDE',
  };

  it('does not ask, and sends the enquiry to the agency the console named', async () => {
    const { fixture, form, submitted } = await withFormUrl('', AGENCIES, [], { askAgency: false, defaultAgency: 'NPC' });
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('.agency-choice')).toBeNull();
    expect(form.value['agency']).toBe('NPC');

    form.patchValue(VALID);
    (fixture.componentInstance as unknown as { submit(): void }).submit();
    expect(submitted()?.get('agency')).toBe('NPC');
    // And again after the form is cleared for the next enquiry.
    expect(form.value['agency']).toBe('NPC');
  });

  it('with no agency named, still lets the form be sent - to the general address', async () => {
    const { form } = await withFormUrl('', AGENCIES, [], { askAgency: false, defaultAgency: null });

    form.patchValue(VALID);
    expect(form.valid).toBe(true);
    expect(form.value['agency']).toBe('');
  });

  it("shows QCI's grievance lists when QCI is the agency named", async () => {
    const { fixture } = await withFormUrl('', AGENCIES, [MATRIX], { askAgency: false, defaultAgency: 'QCI' });

    expect((fixture.nativeElement as HTMLElement).querySelector('fieldset.grievance')).not.toBeNull();
  });
});
