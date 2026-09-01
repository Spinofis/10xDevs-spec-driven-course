import { HttpErrorResponse } from '@angular/common/http';

import { ApiErrorVm } from './trips.models';

interface ProblemPayload {
  detail?: string;
  correlationId?: string;
  firstErrorMessage?: string;
  firstErrorTarget?: string;
}

export function mapHttpErrorToApiError(error: unknown, fallbackMessage: string): ApiErrorVm {
  if (error instanceof HttpErrorResponse) {
    const problem = readProblemPayload(error.error);
    const message = problem.detail ?? problem.firstErrorMessage ?? fallbackMessage;
    const correlationId = problem.correlationId ?? error.headers.get('X-Correlation-Id') ?? undefined;

    if (error.status === 400) {
      return {
        code: 'VALIDATION_ERROR',
        message,
        field: problem.firstErrorTarget,
        correlationId,
        canClearFilters: true,
      };
    }

    if (error.status === 401) {
      return {
        code: 'UNAUTHORIZED',
        message: problem.detail ?? 'Nie masz dostepu do listy wycieczek.',
        correlationId,
        canClearFilters: false,
      };
    }

    if (error.status === 404) {
      return {
        code: 'NOT_FOUND',
        message,
        field: problem.firstErrorTarget,
        correlationId,
        canClearFilters: false,
      };
    }
  }

  return {
    code: 'UNKNOWN',
    message: fallbackMessage,
    canClearFilters: false,
  };
}

function readProblemPayload(payload: unknown): ProblemPayload {
  if (!isRecord(payload)) {
    return {};
  }

  const errors = Array.isArray(payload['errors']) ? payload['errors'] : [];
  const firstError = errors.find(isRecord);

  return {
    detail: readString(payload, 'detail') ?? readString(payload, 'Detail'),
    correlationId: readString(payload, 'correlationId') ?? readString(payload, 'CorrelationId'),
    firstErrorMessage: firstError
      ? readString(firstError, 'message') ?? readString(firstError, 'Message')
      : undefined,
    firstErrorTarget: firstError ? readString(firstError, 'target') ?? readString(firstError, 'Target') : undefined,
  };
}

function readString(source: Record<string, unknown>, key: string): string | undefined {
  const value = source[key];

  return typeof value === 'string' && value.trim().length > 0 ? value : undefined;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
