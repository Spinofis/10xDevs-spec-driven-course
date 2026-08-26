import { Component, ChangeDetectionStrategy } from '@angular/core';
import { AppShellComponent } from './layout/app-shell/app-shell.component';

@Component({
    selector: 'app-root',
    imports: [AppShellComponent],
    templateUrl: './app.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './app.component.sass'
})
export class AppComponent {
  title = 'VibeTravels';
}
