import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MainNavigationComponent } from '../main-navigation/main-navigation.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [MainNavigationComponent, RouterLink, RouterOutlet],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.sass',
})
export class AppShellComponent {}
