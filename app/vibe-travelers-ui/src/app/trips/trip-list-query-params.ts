import { ParamMap } from '@angular/router';

import {
  ApiErrorVm,
  CursorPageState,
  HasPlanFilter,
  TripListFiltersVm,
  TripSort,
} from './trips.models';

export const TRIP_SEARCH_MAX_LENGTH = 200;
export const DEFAULT_TRIP_LIST_FILTERS: TripListFiltersVm = {
  q: '',
  hasPlan: '',
  sort: '-createdAt',
  limit: 20,
};
export const DEFAULT_CURSOR_PAGE_STATE: CursorPageState = {
  currentCursor: null,
  nextCursor: null,
  previousCursors: [],
  pageIndex: 1,
};
export const TRIP_LIMIT_OPTIONS = [10, 20, 50, 100] as const;
export const TRIP_SORT_VALUES = [
  'createdAt',
  '-createdAt',
  'generatedAt',
  '-generatedAt',
  'title',
  '-title',
] as const satisfies readonly TripSort[];

export interface ParsedTripListQueryParams {
  filters: TripListFiltersVm;
  cursor: string | null;
  error: ApiErrorVm | null;
}

export function parseTripListQueryParams(paramMap: ParamMap): ParsedTripListQueryParams {
  const q = parseSearchQuery(paramMap.get('q'));
  if (q.error) {
    return parsedWithError(q.filters, parseCursor(paramMap), q.error);
  }

  const hasPlan = parseHasPlan(paramMap.get('hasPlan'));
  if (hasPlan.error) {
    return parsedWithError({ ...q.filters, hasPlan: '' }, parseCursor(paramMap), hasPlan.error);
  }

  const sort = parseSort(paramMap.get('sort'));
  if (sort.error) {
    return parsedWithError(
      { ...q.filters, hasPlan: hasPlan.value, sort: DEFAULT_TRIP_LIST_FILTERS.sort },
      parseCursor(paramMap),
      sort.error,
    );
  }

  const limit = parseLimit(paramMap.get('limit'));
  if (limit.error) {
    return parsedWithError(
      { ...q.filters, hasPlan: hasPlan.value, sort: sort.value, limit: DEFAULT_TRIP_LIST_FILTERS.limit },
      parseCursor(paramMap),
      limit.error,
    );
  }

  return {
    filters: {
      q: q.filters.q,
      hasPlan: hasPlan.value,
      sort: sort.value,
      limit: limit.value,
    },
    cursor: parseCursor(paramMap),
    error: null,
  };
}

export function buildTripListQueryParams(
  filters: TripListFiltersVm,
  cursor: string | null,
): Record<string, string | number> {
  const queryParams: Record<string, string | number> = {};
  const q = filters.q.trim();

  if (q.length > 0) {
    queryParams['q'] = q;
  }

  if (filters.hasPlan !== '') {
    queryParams['hasPlan'] = filters.hasPlan;
  }

  if (filters.sort !== DEFAULT_TRIP_LIST_FILTERS.sort) {
    queryParams['sort'] = filters.sort;
  }

  if (filters.limit !== DEFAULT_TRIP_LIST_FILTERS.limit) {
    queryParams['limit'] = filters.limit;
  }

  if (cursor) {
    queryParams['cursor'] = cursor;
  }

  return queryParams;
}

export function areTripListFiltersEqual(left: TripListFiltersVm, right: TripListFiltersVm): boolean {
  return (
    left.q === right.q &&
    left.hasPlan === right.hasPlan &&
    left.sort === right.sort &&
    left.limit === right.limit
  );
}

export function hasActiveTripListFilters(filters: TripListFiltersVm): boolean {
  return (
    filters.q.trim().length > 0 ||
    filters.hasPlan !== '' ||
    filters.sort !== DEFAULT_TRIP_LIST_FILTERS.sort ||
    filters.limit !== DEFAULT_TRIP_LIST_FILTERS.limit
  );
}

export function toValidationError(message: string, field?: string): ApiErrorVm {
  return {
    code: 'VALIDATION_ERROR',
    message,
    field,
    canClearFilters: true,
  };
}

function parseSearchQuery(value: string | null): { filters: TripListFiltersVm; error: ApiErrorVm | null } {
  const normalized = (value ?? '').trim();

  if (normalized.length > TRIP_SEARCH_MAX_LENGTH) {
    return {
      filters: DEFAULT_TRIP_LIST_FILTERS,
      error: toValidationError('Wyszukiwanie moze miec maksymalnie 200 znakow.', 'q'),
    };
  }

  return {
    filters: {
      ...DEFAULT_TRIP_LIST_FILTERS,
      q: normalized,
    },
    error: null,
  };
}

function parseHasPlan(value: string | null): { value: HasPlanFilter; error: ApiErrorVm | null } {
  if (value === null || value === '') {
    return { value: '', error: null };
  }

  if (value === 'true' || value === 'false') {
    return { value, error: null };
  }

  return {
    value: '',
    error: toValidationError('Status planu w adresie URL jest niepoprawny.', 'hasPlan'),
  };
}

function parseSort(value: string | null): { value: TripSort; error: ApiErrorVm | null } {
  if (value === null || value === '') {
    return { value: DEFAULT_TRIP_LIST_FILTERS.sort, error: null };
  }

  if (isTripSort(value)) {
    return { value, error: null };
  }

  return {
    value: DEFAULT_TRIP_LIST_FILTERS.sort,
    error: toValidationError('Sortowanie w adresie URL jest niepoprawne.', 'sort'),
  };
}

function parseLimit(value: string | null): { value: number; error: ApiErrorVm | null } {
  if (value === null || value === '') {
    return { value: DEFAULT_TRIP_LIST_FILTERS.limit, error: null };
  }

  const parsedValue = Number(value);

  if (Number.isInteger(parsedValue) && parsedValue >= 1 && parsedValue <= 100) {
    return { value: parsedValue, error: null };
  }

  return {
    value: DEFAULT_TRIP_LIST_FILTERS.limit,
    error: toValidationError('Limit wynikow w adresie URL musi byc liczba od 1 do 100.', 'limit'),
  };
}

function parseCursor(paramMap: ParamMap): string | null {
  const cursor = paramMap.get('cursor');

  return cursor && cursor.trim().length > 0 ? cursor : null;
}

function isTripSort(value: string): value is TripSort {
  return TRIP_SORT_VALUES.some((sortValue) => sortValue === value);
}

function parsedWithError(
  filters: TripListFiltersVm,
  cursor: string | null,
  error: ApiErrorVm,
): ParsedTripListQueryParams {
  return {
    filters,
    cursor,
    error,
  };
}
