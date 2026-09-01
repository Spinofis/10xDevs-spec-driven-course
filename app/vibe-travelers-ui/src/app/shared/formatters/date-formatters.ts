const dateOnlyFormatter = new Intl.DateTimeFormat('pl-PL', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
});
const dateTimeFormatter = new Intl.DateTimeFormat('pl-PL', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});
const dateOnlyPattern = /^(\d{4})-(\d{2})-(\d{2})$/;

export function formatDateRangeLabel(dateFrom: string | null, dateTo: string | null, fallback = 'Brak dat'): string {
  const fromLabel = formatDateOnlyLabel(dateFrom);
  const toLabel = formatDateOnlyLabel(dateTo);

  if (fromLabel && toLabel) {
    return `${fromLabel} - ${toLabel}`;
  }

  if (fromLabel) {
    return `Od ${fromLabel}`;
  }

  if (toLabel) {
    return `Do ${toLabel}`;
  }

  return fallback;
}

export function formatDateOnlyLabel(value: string | null): string | null {
  const parsedDate = parseDateOnly(value);

  return parsedDate ? dateOnlyFormatter.format(parsedDate) : null;
}

export function formatDateTimeLabel(value: string | null, fallback: string): string {
  const parsedDate = parseDateTime(value);

  return parsedDate ? dateTimeFormatter.format(parsedDate) : fallback;
}

function parseDateOnly(value: string | null): Date | null {
  if (!value) {
    return null;
  }

  const match = dateOnlyPattern.exec(value);

  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const monthIndex = Number(match[2]) - 1;
  const day = Number(match[3]);
  const parsedDate = new Date(year, monthIndex, day);

  if (
    parsedDate.getFullYear() !== year ||
    parsedDate.getMonth() !== monthIndex ||
    parsedDate.getDate() !== day
  ) {
    return null;
  }

  return parsedDate;
}

function parseDateTime(value: string | null): Date | null {
  if (!value) {
    return null;
  }

  const parsedDate = new Date(value);

  return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
}
