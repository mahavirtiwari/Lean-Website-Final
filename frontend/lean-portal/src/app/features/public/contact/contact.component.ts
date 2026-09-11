import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GrievanceMatrix, GrievanceOption, Partner } from '../../../core/models/content.models';
import { ContentService } from '../../../core/services/content.service';
import { UiService } from '../../../core/services/ui.service';
import { IconComponent } from '../../../shared/components/icon.component';
import { CmsBannerComponent } from '../../../shared/components/cms-banner.component';
import { CaptchaComponent } from '../../../shared/components/captcha.component';

/** Used only until site settings arrive, and as the fallback if the key is cleared. */
const ENQUIRY_CATEGORIES = [
  'Scheme information',
  'Registration and Udyam',
  'Handholding and consultants',
  'Certification',
  'Technical / portal issue',
  'Grievance',
  'Other',
];

@Component({
  selector: 'app-contact',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, CmsBannerComponent, IconComponent, CaptchaComponent],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss',
})
export class ContactComponent {
  private readonly fb = inject(FormBuilder);
  private readonly content = inject(ContentService);
  private readonly ui = inject(UiService);

  protected readonly settings = this.content.settings;
  protected readonly categories = computed(() =>
    this.content.settingList('contact.enquiryCategories', ENQUIRY_CATEGORIES),
  );

  protected readonly agencies = signal<Partner[]>([]);

  /** Whether to ask which agency the enquiry is for. Asked until the server says otherwise. */
  protected readonly askAgency = signal(true);

  /** Where an enquiry goes when the question is not asked. */
  private readonly defaultAgency = signal('');

  /** The agencies that classify an enquiry with a grievance matrix, and their matrices. */
  private readonly matrices = signal<GrievanceMatrix[]>([]);

  /** The four choices, mirrored into signals so each list can follow the one before it. */
  private readonly grievance = {
    userType: signal(''),
    issueType: signal(''),
    issueCategory: signal(''),
    issueSubCategory: signal(''),
  };

  /** Set on a submit that was missing a level, and cleared as soon as one is chosen. */
  private readonly grievanceAttempted = signal(false);

  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  /**
   * A form hosted elsewhere - Zoho, or anything else that gives out an embed
   * address - shown in place of the built-in one when `contact.formEmbedUrl` is
   * set in the CMS.
   *
   * Only https is accepted, and the address is checked before it is trusted:
   * this value goes into an iframe src, so an unchecked setting would be a way
   * to frame anything at all on a government page. Anything else is ignored and
   * the built-in form stays, which is the safe way to fail.
   */
  protected readonly chosenAgency = signal<string>('');

  /** The agency the sender picked, matched on the value the radio carried. */
  private readonly chosenAgencyRecord = computed(() => {
    const chosen = this.chosenAgency();
    if (!chosen) return null;
    return this.agencies().find((a) => (a.shortName || a.name) === chosen) ?? null;
  });

  /** The chosen agency's grievance matrix, if it uses one. */
  protected readonly activeMatrix = computed<GrievanceMatrix | null>(
    () => this.matrices().find((m) => m.agency === this.chosenAgency()) ?? null,
  );

  /**
   * The lists to show: the first always, each after it only once the one before
   * has an answer that leads somewhere. A branch that ends early shows fewer.
   */
  protected readonly levels = computed(() => {
    const matrix = this.activeMatrix();
    if (!matrix) return [];

    const controls = ['userType', 'issueType', 'issueCategory', 'issueSubCategory'] as const;
    const shown: { control: (typeof controls)[number]; label: string; options: GrievanceOption[] }[] = [];

    let options: GrievanceOption[] | null | undefined = matrix.options;
    for (const [index, control] of controls.entries()) {
      if (!options?.length) break;
      shown.push({ control, label: matrix.labels[index] || `Level ${index + 1}`, options });

      const chosen = this.grievance[control]();
      options = options.find((o) => o.name === chosen)?.children;
      if (!chosen) break;
    }

    return shown;
  });

  /** True when a matrix is in use and its path has not been followed to the end. */
  private readonly grievanceIncomplete = computed(() => {
    const levels = this.levels();
    if (!levels.length) return false;

    const last = levels[levels.length - 1];
    const chosen = this.grievance[last.control]();
    const leaf = last.options.find((o) => o.name === chosen);
    return !chosen || !!leaf?.children?.length;
  });

  protected readonly grievanceMissing = computed(
    () => this.grievanceAttempted() && this.grievanceIncomplete(),
  );

  protected readonly embeddedForm = computed<SafeResourceUrl | null>(() => {
    // The chosen agency's own form wins over the site-wide one, so QCI and NPC can
    // each run their own and neither has to share the other's.
    const raw = (
      this.chosenAgencyRecord()?.enquiryFormUrl ||
      this.content.setting('contact.formEmbedUrl', '')
    ).trim();
    if (!raw) return null;

    let url: URL;
    try {
      url = new URL(raw);
    } catch {
      return null;
    }

    if (url.protocol !== 'https:') return null;

    return this.sanitizer.bypassSecurityTrustResourceUrl(url.toString());
  });
  /** At most this many files, matching what the server will accept. */
  protected readonly maxFiles = 3;

  private readonly maxFileBytes = 5 * 1024 * 1024;

  protected readonly files = signal<File[]>([]);
  protected readonly fileError = signal<string | null>(null);

  /** The challenge the answer belongs to, kept in step by the captcha component. */
  protected readonly captchaId = signal('');
  private readonly captcha = viewChild(CaptchaComponent);

  protected readonly submitting = signal(false);

  /**
   * Wording for a piece of the page.
   *
   * A key that was never set falls back to the built-in default; a key an editor
   * has deliberately emptied comes back empty and what it labels is not rendered.
   * Clearing the field in the CMS is how a heading or a whole section is removed.
   */
  protected text(key: string, fallback: string): string {
    return this.content.setting(key, fallback);
  }
  protected readonly reference = signal<string | null>(null);

  /**
   * The map beside the address.
   *
   * Uses the address itself when no embed address is configured, which is the
   * common case: Google will place a query without an API key, so a site that has
   * filled in its address gets a map without anyone pasting anything. Same https
   * check as the hosted form, for the same reason - it ends up in an iframe.
   */
  protected readonly mapEmbed = computed<SafeResourceUrl | null>(() => {
    const configured = this.content.setting('contact.mapEmbedUrl', '').trim();

    if (configured) {
      try {
        const url = new URL(configured);
        if (url.protocol !== 'https:') return null;
        return this.sanitizer.bypassSecurityTrustResourceUrl(url.toString());
      } catch {
        return null;
      }
    }

    const query = this.addressLines().join(', ');
    if (!query) return null;

    return this.sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.google.com/maps?q=${encodeURIComponent(query)}&output=embed`,
    );
  });

  /** Where the link under the map goes: the same place, opened properly. */
  protected mapLink(): string {
    const query = this.addressLines().join(', ');
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`;
  }

  protected readonly addressLines = computed(() =>
    [
      this.settings()['contact.addressLine1'],
      this.settings()['contact.addressLine2'],
      [this.settings()['contact.city'], this.settings()['contact.pincode']].filter(Boolean).join(' - '),
      this.settings()['contact.country'],
    ].filter((line): line is string => !!line),
  );

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    // A mobile number, required: the team calls back on these, and a landline in
    // the field was a call that could not be returned.
    phone: ['', [Validators.required, Validators.pattern(/^(\+?91[\s-]?)?[6-9]\d{9}$/)]],
    organisation: ['', [Validators.maxLength(300)]],
    udyamNumber: ['', [Validators.maxLength(60)]],
    category: [''],
    userType: [''],
    issueType: [''],
    issueCategory: [''],
    issueSubCategory: [''],
    // No default: the enquiry goes to one of the two agencies, and which one is a
    // choice the sender has to make rather than one made for them by the order the
    // options happen to be in.
    agency: ['', [Validators.required]],
    subject: ['', [Validators.required, Validators.maxLength(400)]],
    message: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(4000)]],
    captchaAnswer: ['', [Validators.required]],
    // Honeypot: a real visitor never sees or fills this field.
    website: [''],
  });

  constructor() {
    this.ui.setMeta({
      title: 'Contact Us',
      description:
        'Contact the MSME Competitive (LEAN) Scheme team at the Ministry of MSME, or reach the implementing agencies QCI and NPC.',
    });

    this.content.getPartners('ImplementationAgency').subscribe({
      next: (partners) => this.agencies.set(partners),
      error: () => this.agencies.set([]),
    });

    // Mirrored into a signal so the embedded form can react to the choice: the
    // agencies may host their own forms, and which one is shown depends on who the
    // enquiry is for.
    this.form.controls.agency.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.chosenAgency.set(String(value ?? '')));

    this.content.getContactOptions().subscribe({
      next: (options) => {
        this.matrices.set(options.grievance ?? []);

        if (options.askAgency === false) {
          // Nothing to choose: the answer is the console's, and the field that
          // would have required one is not on the page.
          this.askAgency.set(false);
          this.defaultAgency.set(options.defaultAgency ?? '');
          this.form.controls.agency.clearValidators();
          this.applyDefaultAgency();
        }
      },
      error: () => this.matrices.set([]),
    });

    // Each list follows the one before: a changed answer clears the ones after it,
    // which may no longer exist under the new choice.
    const order = ['userType', 'issueType', 'issueCategory', 'issueSubCategory'] as const;
    for (const [index, name] of order.entries()) {
      this.form.controls[name].valueChanges
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((value) => {
          this.grievance[name].set(String(value ?? ''));
          for (const later of order.slice(index + 1)) {
            if (this.form.controls[later].value) this.form.controls[later].setValue('');
          }
        });
    }

    // A different agency means a different matrix, or none: start the lists again.
    this.form.controls.agency.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.form.controls.userType.setValue(''));

    // A list with a single option has only one answer; choosing it for the sender
    // saves a click that could only go one way.
    effect(() => {
      for (const level of this.levels()) {
        const control = this.form.controls[level.control];
        if (level.options.length === 1 && !this.grievance[level.control]()) {
          control.setValue(level.options[0].name);
        }
      }
    });

  }

  /** With the question not asked, the agency is the console's - after a reset as well. */
  private applyDefaultAgency(): void {
    if (this.askAgency()) return;
    const agency = this.form.controls.agency;
    agency.setValue(this.defaultAgency());
    agency.updateValueAndValidity();
  }

  /** True once the control has been touched and is invalid - drives the error text. */
  protected invalid(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected errorFor(name: keyof typeof this.form.controls): string {
    const control = this.form.controls[name];
    if (control.hasError('required')) return 'This field is required.';
    if (control.hasError('email')) return 'Enter a valid e-mail address.';
    if (control.hasError('pattern')) {
      return name === 'phone'
        ? 'Enter a 10-digit Indian mobile number.'
        : 'Please check the format of this field.';
    }
    if (control.hasError('minlength')) {
      return `Please write at least ${control.getError('minlength').requiredLength} characters.`;
    }
    if (control.hasError('maxlength')) {
      return `Please keep this under ${control.getError('maxlength').requiredLength} characters.`;
    }
    return 'Please check this field.';
  }

  /**
   * Checks the picked files before they are anywhere near a request.
   *
   * The server checks all of this again - a browser is not a place to enforce
   * anything - but telling someone here saves them filling in a form and losing it
   * to a file they could have been warned about.
   */
  protected onFiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const picked = [...(input.files ?? [])];

    if (picked.length > this.maxFiles) {
      this.fileError.set(`Please attach no more than ${this.maxFiles} files.`);
      this.files.set([]);
      input.value = '';
      return;
    }

    const tooBig = picked.find((file) => file.size > this.maxFileBytes);
    if (tooBig) {
      this.fileError.set(`${tooBig.name} is larger than 5 MB.`);
      this.files.set([]);
      input.value = '';
      return;
    }

    this.fileError.set(null);
    this.files.set(picked);
  }

  protected fileSize(bytes: number): string {
    return bytes < 1024 * 1024
      ? `${Math.max(1, Math.round(bytes / 1024))} KB`
      : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  /** A fresh challenge: each is good for one attempt, accepted or not. */
  private newCaptcha(): void {
    this.captcha()?.refresh();
    this.form.controls.captchaAnswer.setValue('');
  }

  protected submit(): void {
    this.grievanceAttempted.set(true);

    if (this.form.invalid || this.grievanceIncomplete()) {
      this.form.markAllAsTouched();
      this.ui.error('Please correct the highlighted fields and try again.');
      return;
    }

    this.submitting.set(true);
    this.reference.set(null);

    const values = this.form.getRawValue();
    const payload = new FormData();

    for (const [key, value] of Object.entries(values)) {
      payload.append(key, value ?? '');
    }

    payload.append('captchaId', this.captchaId());

    for (const file of this.files()) {
      payload.append('attachments', file, file.name);
    }

    this.content.submitEnquiry(payload).subscribe({
      next: (result) => {
        this.ui.success(result.message);
        this.reference.set(result.reference ?? null);
        this.form.reset();
        this.applyDefaultAgency();
        this.grievanceAttempted.set(false);
        this.files.set([]);
        this.submitting.set(false);

        // A challenge is spent once it is checked, so the next enquiry needs a new
        // one whether this one was accepted or not.
        this.newCaptcha();
      },
      error: () => {
        this.submitting.set(false);
        this.newCaptcha();
      },
    });
  }
}
