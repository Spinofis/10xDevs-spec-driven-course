import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';

import { TripListFiltersForm } from '../trip-list-form.model';
import { TripSelectOption } from '../trip-list-options';
import { HasPlanFilter, TripSort } from '../trips.models';

@Component({
  selector: 'app-trip-list-toolbar',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './trip-list-toolbar.component.html',
  styleUrl: './trip-list-toolbar.component.sass',
})
export class TripListToolbarComponent {
  @Input({ required: true }) filtersForm!: TripListFiltersForm;
  @Input({ required: true }) hasPlanOptions: readonly TripSelectOption<HasPlanFilter>[] = [];
  @Input({ required: true }) sortOptions: readonly TripSelectOption<TripSort>[] = [];
  @Input({ required: true }) limitOptions: readonly number[] = [];
  @Input() isLoading = false;
  @Input() hasActiveFilters = false;
  @Input() validationMessage: string | null = null;

  @Output() filtersSubmit = new EventEmitter<void>();
  @Output() clearFilters = new EventEmitter<void>();
}
