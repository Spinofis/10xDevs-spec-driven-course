import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MainNavigationComponent } from '../main-navigation/main-navigation.component';

@Component({
    selector: 'app-shell',
    imports: [MainNavigationComponent, RouterLink, RouterOutlet],
    templateUrl: './app-shell.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './app-shell.component.sass'
})
export class AppShellComponent {}
