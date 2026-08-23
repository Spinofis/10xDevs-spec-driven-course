import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TripContextPanelComponent } from '../../trips/trip-context-panel/trip-context-panel.component';
import { TripWorkspaceNavComponent } from '../../trips/trip-workspace-nav/trip-workspace-nav.component';

@Component({
  selector: 'app-trip-plan-page',
  standalone: true,
  imports: [TripContextPanelComponent, TripWorkspaceNavComponent],
  templateUrl: './trip-plan-page.component.html',
})
export class TripPlanPageComponent {
  private readonly route = inject(ActivatedRoute);

  readonly tripId = this.route.snapshot.paramMap.get('tripId') ?? '';
}
