import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { toHttpParams } from '../shared/http/http-params.util';
import { ListTripsRequestParams, ListTripsResponse } from './trips.models';

@Injectable({ providedIn: 'root' })
export class TripsApiService {
  private readonly http = inject(HttpClient);
  private readonly tripsUrl = '/trips';

  listTrips(params: ListTripsRequestParams = {}): Observable<ListTripsResponse> {
    return this.http.get<ListTripsResponse>(this.tripsUrl, {
      params: toHttpParams({
        ...params,
        q: params.q?.trim() || undefined
      })
    });
  }

  deleteTrip(tripId: string): Observable<void> {
    return this.http.delete<void>(`${this.tripsUrl}/${encodeURIComponent(tripId)}`);
  }
}
