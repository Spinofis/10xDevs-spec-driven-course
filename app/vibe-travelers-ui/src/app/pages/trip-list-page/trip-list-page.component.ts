import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ConfirmDeleteTripDialogComponent } from '../../trips/confirm-delete-trip-dialog/confirm-delete-trip-dialog.component';
import { TripListComponent } from '../../trips/trip-list/trip-list.component';
import { TripListPaginationComponent } from '../../trips/trip-list-pagination/trip-list-pagination.component';
import { TRIP_LIMIT_OPTIONS } from '../../trips/trip-list-query-params';
import { TripListStateBannerComponent } from '../../trips/trip-list-state-banner/trip-list-state-banner.component';
import { TripListStore } from '../../trips/trip-list-store.service';
import { TRIP_HAS_PLAN_OPTIONS, TRIP_SORT_OPTIONS } from '../../trips/trip-list-options';
import { TripListToolbarComponent } from '../../trips/trip-list-toolbar/trip-list-toolbar.component';
import { TripListItemVm } from '../../trips/trips.models';

@Component({
  selector: 'app-trip-list-page',
  imports: [
    ConfirmDeleteTripDialogComponent,
    RouterLink,
    TripListComponent,
    TripListPaginationComponent,
    TripListStateBannerComponent,
    TripListToolbarComponent,
  ],
  providers: [TripListStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './trip-list-page.component.html',
})
export class TripListPageComponent {
  private readonly store = inject(TripListStore);

  readonly filtersForm = this.store.filtersForm;
  readonly hasPlanOptions = TRIP_HAS_PLAN_OPTIONS;
  readonly sortOptions = TRIP_SORT_OPTIONS;
  readonly limitOptions = TRIP_LIMIT_OPTIONS;
  readonly items = this.store.items;
  readonly isLoading = this.store.isLoading;
  readonly error = this.store.error;
  readonly deleteError = this.store.deleteError;
  readonly pagination = this.store.pagination;
  readonly deletingTripId = this.store.deletingTripId;
  readonly pendingDelete = this.store.pendingDelete;
  readonly hasActiveFilters = this.store.hasActiveFilters;
  readonly canGoBack = this.store.canGoBack;
  readonly isPendingDeleteInProgress = this.store.isPendingDeleteInProgress;
  readonly resultBadge = this.store.resultBadge;
  readonly validationMessage = this.store.validationMessage;

  submitFilters(): void {
    this.store.submitFilters();
  }

  clearFilters(): void {
    this.store.clearFilters();
  }

  retry(): void {
    this.store.retry();
  }

  openTrip(item: TripListItemVm): void {
    this.store.openTrip(item);
  }

  goNext(): void {
    this.store.goNext();
  }

  goPrevious(): void {
    this.store.goPrevious();
  }

  requestDelete(item: TripListItemVm): void {
    this.store.requestDelete(item);
  }

  cancelDelete(): void {
    this.store.cancelDelete();
  }

  confirmDelete(): void {
    this.store.confirmDelete();
  }

}
