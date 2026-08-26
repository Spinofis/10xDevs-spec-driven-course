import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-trip-context-panel',
  standalone: true,
  templateUrl: './trip-context-panel.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './trip-context-panel.component.sass',
})
export class TripContextPanelComponent {
  @Input({ required: true }) tripId = '';
}
