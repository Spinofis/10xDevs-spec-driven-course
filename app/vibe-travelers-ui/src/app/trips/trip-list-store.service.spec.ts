import { HttpErrorResponse } from '@angular/common/http';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, convertToParamMap } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';

import { TripListStore } from './trip-list-store.service';
import { TripsApiService } from './trips-api.service';
import { DEFAULT_TRIP_LIST_FILTERS } from './trip-list-query-params';
import { ListTripsRequestParams, ListTripsResponse, TripDto } from './trips.models';

describe('TripListStore', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the first page from route query params', () => {
    const trip = createTripDto();
    const { store, queryParamMap$, tripsApi } = setupStore({ items: [trip], nextCursor: 'cursor-2' });

    queryParamMap$.next(convertToParamMap({}));

    expect(tripsApi.listTrips).toHaveBeenCalledOnceWith({ limit: 20, sort: '-createdAt' });
    expect(store.items().length).toBe(1);
    expect(store.items()[0].id).toBe(trip.id);
    expect(store.pagination().nextCursor).toBe('cursor-2');
    expect(store.isLoading()).toBeFalse();
  });

  it('does not call the API for invalid query params', () => {
    const { store, queryParamMap$, tripsApi } = setupStore();

    queryParamMap$.next(convertToParamMap({ limit: '101' }));

    expect(tripsApi.listTrips).not.toHaveBeenCalled();
    expect(store.error()?.code).toBe('VALIDATION_ERROR');
    expect(store.error()?.field).toBe('limit');
    expect(store.items()).toEqual([]);
  });

  it('writes filter changes to the URL from the first page', fakeAsync(() => {
    const { store, queryParamMap$, router } = setupStore();

    queryParamMap$.next(convertToParamMap({}));
    router.navigate.calls.reset();

    store.filtersForm.controls.q.setValue('  Lizbona  ');
    tick(300);

    expectLastNavigationQueryParams(router, { q: 'Lizbona' });
    expect(store.pagination().currentCursor).toBeNull();
    expect(store.pagination().previousCursors).toEqual([]);
  }));

  it('clears filters and cursor from the URL', () => {
    const { store, router } = setupStore();

    store.filtersForm.setValue({
      ...DEFAULT_TRIP_LIST_FILTERS,
      q: 'Porto',
      hasPlan: 'true',
      sort: 'title',
      limit: 50,
    });

    store.clearFilters();

    expect(store.filtersForm.getRawValue()).toEqual(DEFAULT_TRIP_LIST_FILTERS);
    expectLastNavigationQueryParams(router, {});
  });

  it('navigates forward and back using cursor pagination', () => {
    const { store, queryParamMap$, router } = setupStore({ items: [createTripDto()], nextCursor: 'cursor-2' });

    queryParamMap$.next(convertToParamMap({}));
    router.navigate.calls.reset();

    store.goNext();

    expect(store.pagination().currentCursor).toBe('cursor-2');
    expect(store.pagination().previousCursors).toEqual([null]);
    expect(store.pagination().pageIndex).toBe(2);
    expectLastNavigationQueryParams(router, { cursor: 'cursor-2' });

    store.goPrevious();

    expect(store.pagination().currentCursor).toBeNull();
    expect(store.pagination().previousCursors).toEqual([]);
    expect(store.pagination().pageIndex).toBe(1);
    expectLastNavigationQueryParams(router, {});
  });

  it('deletes a trip and refreshes the current page', () => {
    const trip = createTripDto();
    const { store, queryParamMap$, tripsApi } = setupStore({ items: [trip], nextCursor: null });

    queryParamMap$.next(convertToParamMap({}));
    tripsApi.listTrips.and.returnValue(of({ items: [], nextCursor: null }));

    store.requestDelete(store.items()[0]);
    store.confirmDelete();

    expect(tripsApi.deleteTrip).toHaveBeenCalledOnceWith(trip.id);
    expect(store.pendingDelete()).toBeNull();
    expect(store.items()).toEqual([]);
  });

  it('keeps the dialog open when delete fails', () => {
    const trip = createTripDto();
    const { store, queryParamMap$, tripsApi } = setupStore({ items: [trip], nextCursor: null });

    queryParamMap$.next(convertToParamMap({}));
    tripsApi.deleteTrip.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 500, error: { detail: 'Server error' } })),
    );

    store.requestDelete(store.items()[0]);
    store.confirmDelete();

    expect(store.pendingDelete()).toEqual({ id: trip.id, title: trip.title });
    expect(store.deleteError()?.code).toBe('UNKNOWN');
    expect(store.deletingTripId()).toBeNull();
  });
});

interface StoreSetup {
  store: TripListStore;
  queryParamMap$: Subject<ParamMap>;
  router: jasmine.SpyObj<Router>;
  tripsApi: jasmine.SpyObj<TripsApiService>;
}

function setupStore(response: ListTripsResponse = { items: [], nextCursor: null }): StoreSetup {
  const queryParamMap$ = new Subject<ParamMap>();
  const router = jasmine.createSpyObj<Router>('Router', ['navigate']);
  const tripsApi = jasmine.createSpyObj<TripsApiService>('TripsApiService', ['listTrips', 'deleteTrip']);
  const route = {
    queryParamMap: queryParamMap$.asObservable(),
  };

  router.navigate.and.returnValue(Promise.resolve(true));
  tripsApi.listTrips.and.returnValue(of(response));
  tripsApi.deleteTrip.and.returnValue(of(undefined));

  TestBed.configureTestingModule({
    providers: [
      TripListStore,
      { provide: ActivatedRoute, useValue: route },
      { provide: Router, useValue: router },
      { provide: TripsApiService, useValue: tripsApi },
    ],
  });

  return {
    store: TestBed.inject(TripListStore),
    queryParamMap$,
    router,
    tripsApi,
  };
}

function expectLastNavigationQueryParams(
  router: jasmine.SpyObj<Router>,
  expectedQueryParams: Record<string, string | number>,
): void {
  const calls = router.navigate.calls.all();
  const lastCall = calls[calls.length - 1];
  const extras = lastCall.args[1];

  expect(lastCall.args[0]).toEqual([]);
  expect(extras?.queryParams).toEqual(expectedQueryParams);
  expect(extras?.relativeTo).toBeDefined();
}

function createTripDto(overrides: Partial<TripDto> = {}): TripDto {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    userId: '22222222-2222-2222-2222-222222222222',
    title: 'Weekend w Lizbonie',
    placeText: 'Lizbona',
    noteText: 'Notatka',
    dateFrom: null,
    dateTo: null,
    stayLengthMinDays: null,
    stayLengthMaxDays: null,
    peopleCount: null,
    budgetLevel: null,
    pace: null,
    generatedAt: null,
    hasGeneratedPlan: false,
    createdAt: '2026-04-01T10:00:00Z',
    updatedAt: '2026-04-01T10:00:00Z',
    ...overrides,
  };
}
