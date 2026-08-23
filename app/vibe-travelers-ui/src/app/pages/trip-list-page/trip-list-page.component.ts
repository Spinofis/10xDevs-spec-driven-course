import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-trip-list-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './trip-list-page.component.html',
})
export class TripListPageComponent {}
