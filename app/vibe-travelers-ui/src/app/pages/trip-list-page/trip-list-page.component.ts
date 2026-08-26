import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-trip-list-page',
    imports: [RouterLink],
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: './trip-list-page.component.html'
})
export class TripListPageComponent {}
