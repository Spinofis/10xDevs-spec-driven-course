import { FormControl, FormGroup } from '@angular/forms';

import { HasPlanFilter, TripSort } from './trips.models';

export type TripListFiltersForm = FormGroup<{
  q: FormControl<string>;
  hasPlan: FormControl<HasPlanFilter>;
  sort: FormControl<TripSort>;
  limit: FormControl<number>;
}>;
