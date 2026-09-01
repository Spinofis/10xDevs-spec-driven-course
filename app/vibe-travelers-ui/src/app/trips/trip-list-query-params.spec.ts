import { convertToParamMap } from '@angular/router';

import {
  DEFAULT_TRIP_LIST_FILTERS,
  buildTripListQueryParams,
  parseTripListQueryParams,
} from './trip-list-query-params';

describe('trip list query params', () => {
  it('parses defaults from an empty param map', () => {
    const parsed = parseTripListQueryParams(convertToParamMap({}));

    expect(parsed.filters).toEqual(DEFAULT_TRIP_LIST_FILTERS);
    expect(parsed.cursor).toBeNull();
    expect(parsed.error).toBeNull();
  });

  it('parses and normalizes valid query params', () => {
    const parsed = parseTripListQueryParams(
      convertToParamMap({
        q: '  Lizbona  ',
        hasPlan: 'false',
        sort: 'title',
        limit: '50',
        cursor: 'opaque-cursor',
      }),
    );

    expect(parsed.filters).toEqual({
      q: 'Lizbona',
      hasPlan: 'false',
      sort: 'title',
      limit: 50,
    });
    expect(parsed.cursor).toBe('opaque-cursor');
    expect(parsed.error).toBeNull();
  });

  it('rejects too long search text', () => {
    const parsed = parseTripListQueryParams(convertToParamMap({ q: 'x'.repeat(201) }));

    expect(parsed.error?.code).toBe('VALIDATION_ERROR');
    expect(parsed.error?.field).toBe('q');
    expect(parsed.filters).toEqual(DEFAULT_TRIP_LIST_FILTERS);
  });

  it('rejects invalid hasPlan value', () => {
    const parsed = parseTripListQueryParams(convertToParamMap({ hasPlan: 'yes' }));

    expect(parsed.error?.code).toBe('VALIDATION_ERROR');
    expect(parsed.error?.field).toBe('hasPlan');
  });

  it('rejects invalid sort value', () => {
    const parsed = parseTripListQueryParams(convertToParamMap({ sort: 'updatedAt' }));

    expect(parsed.error?.code).toBe('VALIDATION_ERROR');
    expect(parsed.error?.field).toBe('sort');
    expect(parsed.filters.sort).toBe(DEFAULT_TRIP_LIST_FILTERS.sort);
  });

  it('rejects limits outside the API range', () => {
    const parsed = parseTripListQueryParams(convertToParamMap({ limit: '101' }));

    expect(parsed.error?.code).toBe('VALIDATION_ERROR');
    expect(parsed.error?.field).toBe('limit');
    expect(parsed.filters.limit).toBe(DEFAULT_TRIP_LIST_FILTERS.limit);
  });

  it('builds compact query params and omits defaults', () => {
    const queryParams = buildTripListQueryParams(
      {
        q: '  Porto  ',
        hasPlan: 'true',
        sort: '-generatedAt',
        limit: 50,
      },
      'next-cursor',
    );

    expect(queryParams).toEqual({
      q: 'Porto',
      hasPlan: 'true',
      sort: '-generatedAt',
      limit: 50,
      cursor: 'next-cursor',
    });
    expect(buildTripListQueryParams(DEFAULT_TRIP_LIST_FILTERS, null)).toEqual({});
  });
});
