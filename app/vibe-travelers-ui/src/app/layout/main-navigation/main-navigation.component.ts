import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface NavigationItem {
  readonly label: string;
  readonly path: string;
  readonly exact: boolean;
}

@Component({
  selector: 'app-main-navigation',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './main-navigation.component.html',
  styleUrl: './main-navigation.component.sass',
})
export class MainNavigationComponent {
  readonly items: readonly NavigationItem[] = [
    { label: 'Wycieczki', path: '/trips', exact: true },
    { label: 'Nowa wycieczka', path: '/trips/new', exact: true },
    { label: 'Preferencje', path: '/preferences', exact: true },
  ];
}
