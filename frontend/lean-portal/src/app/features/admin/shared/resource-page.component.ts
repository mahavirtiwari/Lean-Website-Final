import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { UiService } from '../../../core/services/ui.service';
import { EmptyStateComponent } from '../../../shared/components/ui-widgets';
import { ResourceManagerComponent } from './resource-manager.component';
import { RESOURCE_REGISTRY } from './resource-definitions';

/**
 * Route host for the configuration-driven admin screens. The route segment
 * selects the resource, so one route definition serves every simple resource.
 */
@Component({
  selector: 'app-resource-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ResourceManagerComponent, EmptyStateComponent],
  template: `
    @if (config(); as resource) {
      <app-resource-manager [config]="resource" />
    } @else {
      <app-empty-state
        heading="Unknown section"
        message="That management screen does not exist."
        icon="alert-circle"
      />
    }
  `,
})
export class ResourcePageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly ui = inject(UiService);

  private readonly segment = toSignal(
    this.route.data.pipe(map((data) => (data['resource'] as string | undefined) ?? '')),
    { initialValue: '' },
  );

  protected readonly config = computed(() => {
    const resource = RESOURCE_REGISTRY[this.segment()] ?? null;
    if (resource) this.ui.setMeta({ title: resource.title });
    return resource;
  });
}
