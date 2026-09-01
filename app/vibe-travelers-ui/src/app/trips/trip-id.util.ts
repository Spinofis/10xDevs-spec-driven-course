const tripIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function isValidTripId(value: string | null | undefined): value is string {
  return typeof value === 'string' && tripIdPattern.test(value.trim());
}
