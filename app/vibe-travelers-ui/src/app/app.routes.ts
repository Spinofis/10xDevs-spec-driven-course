import { Routes } from '@angular/router';

import { NewTripPageComponent } from './pages/new-trip-page/new-trip-page.component';
import { PreferencesPageComponent } from './pages/preferences-page/preferences-page.component';
import { TripDetailsPageComponent } from './pages/trip-details-page/trip-details-page.component';
import { TripListPageComponent } from './pages/trip-list-page/trip-list-page.component';
import { TripPlanPageComponent } from './pages/trip-plan-page/trip-plan-page.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'trips',
  },
  {
    path: 'trips',
    component: TripListPageComponent,
    title: 'Wycieczki | VibeTravels',
  },
  {
    path: 'trips/new',
    component: NewTripPageComponent,
    title: 'Nowa wycieczka | VibeTravels',
  },
  {
    path: 'trips/:tripId/details',
    component: TripDetailsPageComponent,
    title: 'Szczegóły wycieczki | VibeTravels',
  },
  {
    path: 'trips/:tripId/plan',
    component: TripPlanPageComponent,
    title: 'Plan wycieczki | VibeTravels',
  },
  {
    path: 'preferences',
    component: PreferencesPageComponent,
    title: 'Preferencje | VibeTravels',
  },
  {
    path: '**',
    redirectTo: 'trips',
  },
];
