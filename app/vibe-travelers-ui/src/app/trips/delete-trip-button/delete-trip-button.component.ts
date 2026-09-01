import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-delete-trip-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './delete-trip-button.component.html',
  styleUrl: './delete-trip-button.component.sass',
})
export class DeleteTripButtonComponent {
  @Input({ required: true }) tripTitle = '';
  @Input() disabled = false;
  @Input() isDeleting = false;

  @Output() deleteRequested = new EventEmitter<void>();

  get ariaLabel(): string {
    const action = this.isDeleting ? 'Usuwam' : 'Usun';

    return `${action} wycieczke ${this.tripTitle}`;
  }

  onClick(event: Event): void {
    event.stopPropagation();

    if (this.disabled) {
      return;
    }

    this.deleteRequested.emit();
  }
}
