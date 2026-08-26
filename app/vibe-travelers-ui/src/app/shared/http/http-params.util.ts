import { HttpParams } from '@angular/common/http';

export type HttpParamValue = string | number | boolean | null | undefined;

export function toHttpParams(params: Record<string, HttpParamValue>): HttpParams {
  let httpParams = new HttpParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') {
      return;
    }

    httpParams = httpParams.set(key, String(value));
  });

  return httpParams;
}
