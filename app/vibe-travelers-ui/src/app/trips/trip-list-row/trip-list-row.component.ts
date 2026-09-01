import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

import { DeleteTripButtonComponent } from '../delete-trip-button/delete-trip-button.component';
import { TripListItemVm } from '../trips.models';

@Component({
  selector: 'app-trip-list-row',
  imports: [DeleteTripButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './trip-list-row.component.html',
  styleUrl: './trip-list-row.component.sass',
})
export class TripListRowComponent {
  @Input({ required: true }) item!: TripListItemVm;
  @Input() deleteDisabled = false;
  @Input() isDeleting = false;

  @Output() open = new EventEmitter<TripListItemVm>();
  @Output() deleteRequested = new EventEmitter<TripListItemVm>();

  get hasFullNote(): boolean {
    return Boolean(this.item.noteFullText && this.item.noteFullText !== this.item.notePreview);
  }

  get openAriaLabel(): string {
    return `Otworz szczegoly wycieczki ${this.item.title}`;
  }

  openRow(): void {
    this.open.emit(this.item);
  }

  openRowFromKeyboard(event: KeyboardEvent): void {
    if (event.key !== 'Enter' && event.key !== ' ') {
      return;
    }

    event.preventDefault();
    this.openRow();
  }

  requestDelete(): void {
    this.deleteRequested.emit(this.item);
  }

  stopRowInteraction(event: Event): void {
    event.stopPropagation();
  }
}
