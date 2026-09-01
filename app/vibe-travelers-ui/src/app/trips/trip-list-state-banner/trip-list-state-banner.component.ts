import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';

export type TripListStateBannerState = 'loading' | 'empty' | 'error';

@Component({
  selector: 'app-trip-list-state-banner',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './trip-list-state-banner.component.html',
  styleUrl: './trip-list-state-banner.component.sass',
})
export class TripListStateBannerComponent {
  @Input() state: TripListStateBannerState = 'loading';
  @Input() heading: string | null = null;
  @Input({ required: true }) message = '';
  @Input() correlationId: string | null = null;
  @Input() asPanel = false;
  @Input() canClearFilters = false;
  @Input() canRetry = false;
  @Input() showCreateLink = false;
  @Input() isLoading = false;

  @Output() retry = new EventEmitter<void>();
  @Output() clearFilters = new EventEmitter<void>();

  get role(): 'alert' | 'status' {
    return this.state === 'error' ? 'alert' : 'status';
  }

  get ariaLive(): 'assertive' | 'polite' {
    return this.state === 'error' ? 'assertive' : 'polite';
  }
}
