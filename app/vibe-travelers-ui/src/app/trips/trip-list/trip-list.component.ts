import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ApiErrorVm, TripListItemVm } from '../trips.models';
import { TripListRowComponent } from '../trip-list-row/trip-list-row.component';

@Component({
  selector: 'app-trip-list',
  imports: [RouterLink, TripListRowComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './trip-list.component.html',
  styleUrl: './trip-list.component.sass',
})
export class TripListComponent {
  @Input({ required: true }) items: readonly TripListItemVm[] = [];
  @Input() isLoading = false;
  @Input() error: ApiErrorVm | null = null;
  @Input() hasActiveFilters = false;
  @Input() deletingTripId: string | null = null;

  @Output() openTrip = new EventEmitter<TripListItemVm>();
  @Output() requestDelete = new EventEmitter<TripListItemVm>();
  @Output() clearFilters = new EventEmitter<void>();

  isDeleting(itemId: string): boolean {
    return this.deletingTripId === itemId;
  }
}
