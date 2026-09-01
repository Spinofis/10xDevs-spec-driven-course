import { formatDateRangeLabel, formatDateTimeLabel } from '../shared/formatters/date-formatters';
import { normalizeNullableText, normalizeText, truncateText } from '../shared/formatters/text-formatters';
import { BudgetLevel, Pace, TripDto, TripListItemVm } from './trips.models';

const emptyLabel = 'Brak danych';
const tripNotePreviewMaxLength = 120;
const budgetLevelLabels: Record<BudgetLevel, string> = {
  low: 'Niski',
  medium: 'Sredni',
  high: 'Wysoki',
};
const paceLabels: Record<Pace, string> = {
  relaxed: 'Spokojne',
  normal: 'Zbalansowane',
  fast: 'Intensywne',
};

export function mapTripDtoToListItemVm(trip: TripDto): TripListItemVm {
  const tripId = trip.id.trim();
  const noteText = normalizeNullableText(trip.noteText);

  return {
    id: tripId,
    title: normalizeText(trip.title, 'Bez tytulu'),
    placeLabel: normalizeText(trip.placeText, 'Brak miejsca'),
    notePreview: noteText ? truncateText(noteText, tripNotePreviewMaxLength) : 'Brak notatki',
    noteFullText: noteText,
    dateRangeLabel: formatDateRangeLabel(trip.dateFrom, trip.dateTo),
    stayLengthLabel: formatStayLength(trip.stayLengthMinDays, trip.stayLengthMaxDays),
    peopleCountLabel: formatPeopleCount(trip.peopleCount),
    budgetLabel: formatBudgetLevel(trip.budgetLevel),
    paceLabel: formatPace(trip.pace),
    planStatusLabel: trip.hasGeneratedPlan ? 'Plan gotowy' : 'Brak planu',
    planStatusTone: trip.hasGeneratedPlan ? 'success' : 'neutral',
    createdAtLabel: formatDateTimeLabel(trip.createdAt, 'Brak daty utworzenia'),
    generatedAtLabel: formatDateTimeLabel(trip.generatedAt, 'Nie wygenerowano'),
    detailsUrl: `/trips/${encodeURIComponent(tripId)}/details`,
  };
}

function formatStayLength(minDays: number | null, maxDays: number | null): string {
  const min = normalizePositiveInteger(minDays);
  const max = normalizePositiveInteger(maxDays);

  if (min !== null && max !== null && min === max) {
    return formatDays(min);
  }

  if (min !== null && max !== null) {
    return `${min}-${max} dni`;
  }

  if (min !== null) {
    return `Od ${formatDays(min).toLowerCase()}`;
  }

  if (max !== null) {
    return `Do ${formatDays(max).toLowerCase()}`;
  }

  return 'Brak dlugosci';
}

function formatPeopleCount(value: number | null): string {
  const peopleCount = normalizePositiveInteger(value);

  if (peopleCount === null) {
    return emptyLabel;
  }

  if (peopleCount === 1) {
    return '1 osoba';
  }

  if (peopleCount >= 2 && peopleCount <= 4) {
    return `${peopleCount} osoby`;
  }

  return `${peopleCount} osob`;
}

function normalizePositiveInteger(value: number | null): number | null {
  if (value === null || !Number.isInteger(value) || value <= 0) {
    return null;
  }

  return value;
}

function formatDays(value: number): string {
  if (value === 1) {
    return '1 dzien';
  }

  return `${value} dni`;
}

function formatBudgetLevel(value: BudgetLevel | null): string {
  return value ? budgetLevelLabels[value] : emptyLabel;
}

function formatPace(value: Pace | null): string {
  return value ? paceLabels[value] : emptyLabel;
}
