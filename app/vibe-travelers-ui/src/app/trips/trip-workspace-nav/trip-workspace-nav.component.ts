import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-trip-workspace-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './trip-workspace-nav.component.html',
  styleUrl: './trip-workspace-nav.component.sass',
})
export class TripWorkspaceNavComponent {
  @Input({ required: true }) tripId = '';
}
