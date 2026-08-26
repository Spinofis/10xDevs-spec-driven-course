import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
    selector: 'app-trip-workspace-nav',
    imports: [RouterLink, RouterLinkActive],
    templateUrl: './trip-workspace-nav.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './trip-workspace-nav.component.sass'
})
export class TripWorkspaceNavComponent {
  @Input({ required: true }) tripId = '';
}
