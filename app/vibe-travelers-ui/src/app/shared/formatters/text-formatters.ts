const defaultEllipsis = '...';

export function normalizeText(value: string | null | undefined, fallback: string): string {
  const normalized = normalizeNullableText(value);

  return normalized ?? fallback;
}

export function normalizeNullableText(value: string | null | undefined): string | null {
  if (value === null || value === undefined) {
    return null;
  }

  const normalized = value.trim();

  return normalized.length > 0 ? normalized : null;
}

export function truncateText(value: string, maxLength: number): string {
  if (value.length <= maxLength) {
    return value;
  }

  if (maxLength <= defaultEllipsis.length) {
    return value.slice(0, Math.max(0, maxLength));
  }

  const visibleTextLength = maxLength - defaultEllipsis.length;

  return `${value.slice(0, visibleTextLength).trimEnd()}${defaultEllipsis}`;
}
