export type BudgetLevel = 'low' | 'medium' | 'high';
export type Pace = 'relaxed' | 'normal' | 'fast';
export type TripSortField = 'createdAt' | 'generatedAt' | 'title';
export type TripSort = TripSortField | `-${TripSortField}`;
export type HasPlanFilter = '' | 'true' | 'false';

export interface TripDto {
  id: string;
  userId: string;
  title: string;
  placeText: string | null;
  noteText: string | null;
  dateFrom: string | null;
  dateTo: string | null;
  stayLengthMinDays: number | null;
  stayLengthMaxDays: number | null;
  peopleCount: number | null;
  budgetLevel: BudgetLevel | null;
  pace: Pace | null;
  generatedAt: string | null;
  hasGeneratedPlan: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ListTripsRequestParams {
  q?: string;
  hasPlan?: boolean;
  includeDeleted?: boolean;
  limit?: number;
  cursor?: string;
  sort?: TripSort;
}

export interface ListTripsResponse {
  items: TripDto[];
  nextCursor: string | null;
}

export interface TripListFiltersVm {
  q: string;
  hasPlan: HasPlanFilter;
  sort: TripSort;
  limit: number;
}

export interface TripListItemVm {
  id: string;
  title: string;
  placeLabel: string;
  notePreview: string;
  noteFullText: string | null;
  dateRangeLabel: string;
  stayLengthLabel: string;
  peopleCountLabel: string;
  budgetLabel: string;
  paceLabel: string;
  planStatusLabel: string;
  planStatusTone: 'success' | 'neutral';
  createdAtLabel: string;
  generatedAtLabel: string;
  detailsUrl: string;
}

export interface CursorPageState {
  currentCursor: string | null;
  nextCursor: string | null;
  previousCursors: Array<string | null>;
  pageIndex: number;
}

export interface PendingDeleteTripVm {
  id: string;
  title: string;
}

export interface ApiErrorVm {
  code: 'VALIDATION_ERROR' | 'NOT_FOUND' | 'UNAUTHORIZED' | 'UNKNOWN';
  message: string;
  field?: string;
  correlationId?: string;
  canClearFilters: boolean;
}

export interface TripListPageState {
  items: TripListItemVm[];
  isLoading: boolean;
  error: ApiErrorVm | null;
  filters: TripListFiltersVm;
  pagination: CursorPageState;
  deletingTripId: string | null;
  pendingDelete: PendingDeleteTripVm | null;
}
