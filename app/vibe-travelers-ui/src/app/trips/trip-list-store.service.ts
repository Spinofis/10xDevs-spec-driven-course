import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, merge } from 'rxjs';

import { TripsApiService } from './trips-api.service';
import { mapHttpErrorToApiError } from './trip-api-error.mapper';
import { isValidTripId } from './trip-id.util';
import { TripListFiltersForm } from './trip-list-form.model';
import { mapTripDtoToListItemVm } from './trip-list.mapper';
import {
  DEFAULT_CURSOR_PAGE_STATE,
  DEFAULT_TRIP_LIST_FILTERS,
  TRIP_SEARCH_MAX_LENGTH,
  areTripListFiltersEqual,
  buildTripListQueryParams,
  hasActiveTripListFilters,
  parseTripListQueryParams,
  toValidationError,
} from './trip-list-query-params';
import {
  ApiErrorVm,
  CursorPageState,
  HasPlanFilter,
  ListTripsRequestParams,
  PendingDeleteTripVm,
  TripListFiltersVm,
  TripListItemVm,
  TripSort,
} from './trips.models';

@Injectable()
export class TripListStore {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly tripsApi = inject(TripsApiService);
  private readonly destroyRef = inject(DestroyRef);

  private loadSequence = 0;

  readonly filtersForm: TripListFiltersForm = new FormGroup({
    q: new FormControl(DEFAULT_TRIP_LIST_FILTERS.q, {
      nonNullable: true,
      validators: [Validators.maxLength(TRIP_SEARCH_MAX_LENGTH)],
    }),
    hasPlan: new FormControl<HasPlanFilter>(DEFAULT_TRIP_LIST_FILTERS.hasPlan, { nonNullable: true }),
    sort: new FormControl<TripSort>(DEFAULT_TRIP_LIST_FILTERS.sort, { nonNullable: true }),
    limit: new FormControl(DEFAULT_TRIP_LIST_FILTERS.limit, { nonNullable: true }),
  });

  readonly filters = signal<TripListFiltersVm>(DEFAULT_TRIP_LIST_FILTERS);
  readonly items = signal<TripListItemVm[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<ApiErrorVm | null>(null);
  readonly deleteError = signal<ApiErrorVm | null>(null);
  readonly pagination = signal<CursorPageState>(DEFAULT_CURSOR_PAGE_STATE);
  readonly deletingTripId = signal<string | null>(null);
  readonly pendingDelete = signal<PendingDeleteTripVm | null>(null);

  readonly hasActiveFilters = computed(() => hasActiveTripListFilters(this.filters()));
  readonly canGoBack = computed(() => this.pagination().previousCursors.length > 0 && !this.isLoading());
  readonly canGoNext = computed(() => Boolean(this.pagination().nextCursor) && !this.isLoading());
  readonly isPendingDeleteInProgress = computed(() => {
    const pendingDelete = this.pendingDelete();

    return pendingDelete !== null && this.deletingTripId() === pendingDelete.id;
  });
  readonly resultBadge = computed(() => {
    if (this.isLoading()) {
      return 'Ladowanie';
    }

    if (this.error()) {
      return 'Blad';
    }

    const count = this.items().length;

    if (count === 1) {
      return '1 wynik';
    }

    return `${count} wynikow`;
  });
  readonly validationMessage = computed(() => {
    if (this.filtersForm.controls.q.hasError('maxlength')) {
      return 'Wyszukiwanie moze miec maksymalnie 200 znakow.';
    }

    const currentError = this.error();

    return currentError?.code === 'VALIDATION_ERROR' ? currentError.message : null;
  });

  constructor() {
    this.bindRouteQueryParams();
    this.bindFormChanges();
  }

  submitFilters(): void {
    this.applyFiltersFromForm(true);
  }

  clearFilters(): void {
    this.filtersForm.setValue(DEFAULT_TRIP_LIST_FILTERS, { emitEvent: false });
    this.error.set(null);
    this.pagination.set(createCursorPageState(null));
    this.navigateWithFilters(DEFAULT_TRIP_LIST_FILTERS, null);
  }

  retry(): void {
    if (this.error()?.code === 'VALIDATION_ERROR') {
      return;
    }

    this.loadTrips(this.filters(), this.pagination().currentCursor);
  }

  openTrip(item: TripListItemVm): void {
    if (!isValidTripId(item.id)) {
      return;
    }

    void this.router.navigate(['/trips', item.id, 'details']);
  }

  goNext(): void {
    const pagination = this.pagination();
    const nextCursor = pagination.nextCursor;

    if (!nextCursor || this.isLoading()) {
      return;
    }

    this.pagination.set({
      currentCursor: nextCursor,
      nextCursor: null,
      previousCursors: [...pagination.previousCursors, pagination.currentCursor],
      pageIndex: pagination.pageIndex + 1,
    });
    this.navigateWithFilters(this.filters(), nextCursor);
  }

  goPrevious(): void {
    const pagination = this.pagination();

    if (pagination.previousCursors.length === 0 || this.isLoading()) {
      return;
    }

    const previousCursors = [...pagination.previousCursors];
    const previousCursor = previousCursors.pop() ?? null;

    this.pagination.set({
      currentCursor: previousCursor,
      nextCursor: null,
      previousCursors,
      pageIndex: Math.max(1, pagination.pageIndex - 1),
    });
    this.navigateWithFilters(this.filters(), previousCursor);
  }

  requestDelete(item: TripListItemVm): void {
    if (!isValidTripId(item.id) || this.isLoading()) {
      return;
    }

    this.deleteError.set(null);
    this.pendingDelete.set({ id: item.id, title: item.title });
  }

  cancelDelete(): void {
    if (this.isPendingDeleteInProgress()) {
      return;
    }

    this.deleteError.set(null);
    this.pendingDelete.set(null);
  }

  confirmDelete(): void {
    const pendingDelete = this.pendingDelete();

    if (!pendingDelete) {
      return;
    }

    if (!isValidTripId(pendingDelete.id)) {
      this.deleteError.set({
        code: 'VALIDATION_ERROR',
        message: 'Nie mozna usunac wycieczki bez poprawnego identyfikatora.',
        field: 'tripId',
        canClearFilters: false,
      });

      return;
    }

    this.deletingTripId.set(pendingDelete.id);
    this.deleteError.set(null);
    this.tripsApi
      .deleteTrip(pendingDelete.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.pendingDelete.set(null);
          this.deletingTripId.set(null);
          this.items.update((items) => items.filter((item) => item.id !== pendingDelete.id));
          this.loadTrips(this.filters(), this.pagination().currentCursor);
        },
        error: (error: unknown) => {
          const mappedError = mapHttpErrorToApiError(error, 'Nie udalo sie usunac wycieczki.');

          this.deletingTripId.set(null);

          if (mappedError.code === 'NOT_FOUND') {
            this.pendingDelete.set(null);
            this.loadTrips(this.filters(), this.pagination().currentCursor);
            this.error.set({
              ...mappedError,
              message: 'Ta wycieczka zostala juz usunieta. Odswiezam liste.',
            });

            return;
          }

          this.deleteError.set(mappedError);
        },
      });
  }

  isDeleting(itemId: string): boolean {
    return this.deletingTripId() === itemId;
  }

  private bindRouteQueryParams(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((paramMap) => {
      const parsed = parseTripListQueryParams(paramMap);
      const filtersChanged = !areTripListFiltersEqual(this.filters(), parsed.filters);

      this.filters.set(parsed.filters);
      this.filtersForm.setValue(parsed.filters, { emitEvent: false });

      if (parsed.error) {
        this.loadSequence += 1;
        this.isLoading.set(false);
        this.items.set([]);
        this.error.set(parsed.error);
        this.pagination.set(createCursorPageState(parsed.cursor));

        return;
      }

      this.error.set(null);
      this.updatePaginationFromUrl(parsed.cursor, filtersChanged);
      this.loadTrips(parsed.filters, parsed.cursor);
    });
  }

  private bindFormChanges(): void {
    this.filtersForm.controls.q.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.applyFiltersFromForm(false));

    merge(
      this.filtersForm.controls.hasPlan.valueChanges,
      this.filtersForm.controls.sort.valueChanges,
      this.filtersForm.controls.limit.valueChanges,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.applyFiltersFromForm(false));
  }

  private applyFiltersFromForm(markTouched: boolean): void {
    if (markTouched) {
      this.filtersForm.markAllAsTouched();
    }

    const filters = this.readFiltersFromForm();

    if (filters.q.length > TRIP_SEARCH_MAX_LENGTH) {
      this.error.set(toValidationError('Wyszukiwanie moze miec maksymalnie 200 znakow.', 'q'));

      return;
    }

    this.pagination.set(createCursorPageState(null));
    this.navigateWithFilters(filters, null);
  }

  private readFiltersFromForm(): TripListFiltersVm {
    return {
      q: this.filtersForm.controls.q.value.trim(),
      hasPlan: this.filtersForm.controls.hasPlan.value,
      sort: this.filtersForm.controls.sort.value,
      limit: this.filtersForm.controls.limit.value,
    };
  }

  private navigateWithFilters(filters: TripListFiltersVm, cursor: string | null): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: buildTripListQueryParams(filters, cursor),
    });
  }

  private updatePaginationFromUrl(cursor: string | null, filtersChanged: boolean): void {
    this.pagination.update((pagination) => {
      if (filtersChanged) {
        return createCursorPageState(cursor);
      }

      if (pagination.currentCursor === cursor) {
        return pagination;
      }

      if (cursor === null) {
        return createCursorPageState(null);
      }

      return {
        ...pagination,
        currentCursor: cursor,
        nextCursor: null,
        pageIndex: Math.max(2, pagination.pageIndex),
      };
    });
  }

  private loadTrips(filters: TripListFiltersVm, cursor: string | null): void {
    const sequence = this.loadSequence + 1;
    this.loadSequence = sequence;
    this.isLoading.set(true);
    this.error.set(null);

    this.tripsApi
      .listTrips(toListTripsRequestParams(filters, cursor))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (sequence !== this.loadSequence) {
            return;
          }

          const items = response.items
            .filter((trip) => isValidTripId(trip.id))
            .map((trip) => mapTripDtoToListItemVm(trip));

          this.items.set(items);
          this.pagination.update((pagination) => ({
            ...pagination,
            currentCursor: cursor,
            nextCursor: response.nextCursor,
          }));
          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          if (sequence !== this.loadSequence) {
            return;
          }

          this.error.set(mapHttpErrorToApiError(error, 'Nie udalo sie pobrac listy wycieczek.'));
          this.isLoading.set(false);
        },
      });
  }
}

function createCursorPageState(cursor: string | null): CursorPageState {
  return {
    currentCursor: cursor,
    nextCursor: null,
    previousCursors: [],
    pageIndex: cursor ? 2 : 1,
  };
}

function toListTripsRequestParams(filters: TripListFiltersVm, cursor: string | null): ListTripsRequestParams {
  const params: ListTripsRequestParams = {
    limit: filters.limit,
    sort: filters.sort,
  };
  const q = filters.q.trim();

  if (q.length > 0) {
    params.q = q;
  }

  if (filters.hasPlan !== '') {
    params.hasPlan = filters.hasPlan === 'true';
  }

  if (cursor) {
    params.cursor = cursor;
  }

  return params;
}
