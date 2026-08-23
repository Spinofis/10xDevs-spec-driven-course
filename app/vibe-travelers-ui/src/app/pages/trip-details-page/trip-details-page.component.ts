import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TripContextPanelComponent } from '../../trips/trip-context-panel/trip-context-panel.component';
import { TripWorkspaceNavComponent } from '../../trips/trip-workspace-nav/trip-workspace-nav.component';

@Component({
  selector: 'app-trip-details-page',
  standalone: true,
  imports: [RouterLink, TripContextPanelComponent, TripWorkspaceNavComponent],
  templateUrl: './trip-details-page.component.html',
})
export class TripDetailsPageComponent {
  private readonly route = inject(ActivatedRoute);

  readonly tripId = this.route.snapshot.paramMap.get('tripId') ?? '';
}
