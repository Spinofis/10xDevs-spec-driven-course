import { mapTripDtoToListItemVm } from './trip-list.mapper';
import { TripDto } from './trips.models';

describe('mapTripDtoToListItemVm', () => {
  it('maps trip details into labels for the list row', () => {
    const trip = createTripDto({
      id: ' 11111111-1111-1111-1111-111111111111 ',
      title: 'Weekend w Lizbonie',
      placeText: 'Lizbona, Portugalia',
      noteText: 'a'.repeat(130),
      dateFrom: '2026-05-10',
      dateTo: '2026-05-14',
      stayLengthMinDays: 4,
      stayLengthMaxDays: 4,
      peopleCount: 2,
      budgetLevel: 'medium',
      pace: 'normal',
      generatedAt: '2026-05-01T12:30:00Z',
      hasGeneratedPlan: true,
    });

    const viewModel = mapTripDtoToListItemVm(trip);

    expect(viewModel.id).toBe('11111111-1111-1111-1111-111111111111');
    expect(viewModel.title).toBe('Weekend w Lizbonie');
    expect(viewModel.placeLabel).toBe('Lizbona, Portugalia');
    expect(viewModel.notePreview.length).toBeLessThanOrEqual(120);
    expect(viewModel.notePreview.endsWith('...')).toBeTrue();
    expect(viewModel.noteFullText).toBe(trip.noteText);
    expect(viewModel.dateRangeLabel).toContain('2026');
    expect(viewModel.stayLengthLabel).toBe('4 dni');
    expect(viewModel.peopleCountLabel).toBe('2 osoby');
    expect(viewModel.budgetLabel).toBe('Sredni');
    expect(viewModel.paceLabel).toBe('Zbalansowane');
    expect(viewModel.planStatusLabel).toBe('Plan gotowy');
    expect(viewModel.planStatusTone).toBe('success');
    expect(viewModel.generatedAtLabel).not.toBe('Nie wygenerowano');
    expect(viewModel.detailsUrl).toBe('/trips/11111111-1111-1111-1111-111111111111/details');
  });

  it('uses neutral labels for nullable or invalid values', () => {
    const trip = createTripDto({
      title: '   ',
      placeText: null,
      noteText: '   ',
      dateFrom: '2026-02-31',
      dateTo: null,
      stayLengthMinDays: null,
      stayLengthMaxDays: 0,
      peopleCount: null,
      budgetLevel: null,
      pace: null,
      generatedAt: null,
      createdAt: 'not-a-date',
      hasGeneratedPlan: false,
    });

    const viewModel = mapTripDtoToListItemVm(trip);

    expect(viewModel.title).toBe('Bez tytulu');
    expect(viewModel.placeLabel).toBe('Brak miejsca');
    expect(viewModel.notePreview).toBe('Brak notatki');
    expect(viewModel.noteFullText).toBeNull();
    expect(viewModel.dateRangeLabel).toBe('Brak dat');
    expect(viewModel.stayLengthLabel).toBe('Brak dlugosci');
    expect(viewModel.peopleCountLabel).toBe('Brak danych');
    expect(viewModel.budgetLabel).toBe('Brak danych');
    expect(viewModel.paceLabel).toBe('Brak danych');
    expect(viewModel.planStatusLabel).toBe('Brak planu');
    expect(viewModel.planStatusTone).toBe('neutral');
    expect(viewModel.createdAtLabel).toBe('Brak daty utworzenia');
    expect(viewModel.generatedAtLabel).toBe('Nie wygenerowano');
  });

  it('formats partial stay and people values without throwing', () => {
    expect(mapTripDtoToListItemVm(createTripDto({ stayLengthMinDays: 3, stayLengthMaxDays: null })).stayLengthLabel)
      .toBe('Od 3 dni');
    expect(mapTripDtoToListItemVm(createTripDto({ stayLengthMinDays: null, stayLengthMaxDays: 5 })).stayLengthLabel)
      .toBe('Do 5 dni');
    expect(mapTripDtoToListItemVm(createTripDto({ peopleCount: 5 })).peopleCountLabel).toBe('5 osob');
  });
});

function createTripDto(overrides: Partial<TripDto> = {}): TripDto {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    userId: '22222222-2222-2222-2222-222222222222',
    title: 'Wycieczka',
    placeText: 'Miejsce',
    noteText: 'Notatka',
    dateFrom: null,
    dateTo: null,
    stayLengthMinDays: null,
    stayLengthMaxDays: null,
    peopleCount: null,
    budgetLevel: null,
    pace: null,
    generatedAt: null,
    hasGeneratedPlan: false,
    createdAt: '2026-04-01T10:00:00Z',
    updatedAt: '2026-04-01T10:00:00Z',
    ...overrides,
  };
}
