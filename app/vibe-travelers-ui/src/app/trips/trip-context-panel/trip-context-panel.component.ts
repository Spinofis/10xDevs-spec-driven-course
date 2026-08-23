import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-trip-context-panel',
  standalone: true,
  templateUrl: './trip-context-panel.component.html',
  styleUrl: './trip-context-panel.component.sass',
})
export class TripContextPanelComponent {
  @Input({ required: true }) tripId = '';
}
