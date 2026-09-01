import { HasPlanFilter, TripSort } from './trips.models';

export interface TripSelectOption<TValue extends string | number> {
  value: TValue;
  label: string;
}

export const TRIP_HAS_PLAN_OPTIONS: readonly TripSelectOption<HasPlanFilter>[] = [
  { value: '', label: 'Wszystkie' },
  { value: 'true', label: 'Z planem' },
  { value: 'false', label: 'Bez planu' },
];

export const TRIP_SORT_OPTIONS: readonly TripSelectOption<TripSort>[] = [
  { value: '-createdAt', label: 'Najnowsze utworzone' },
  { value: 'createdAt', label: 'Najstarsze utworzone' },
  { value: '-generatedAt', label: 'Ostatnio wygenerowane' },
  { value: 'generatedAt', label: 'Najdawniej wygenerowane' },
  { value: 'title', label: 'Tytul A-Z' },
  { value: '-title', label: 'Tytul Z-A' },
];
