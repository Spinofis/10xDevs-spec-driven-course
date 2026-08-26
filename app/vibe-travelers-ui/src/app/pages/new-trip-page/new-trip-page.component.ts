import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-new-trip-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './new-trip-page.component.html',
})
export class NewTripPageComponent {}
