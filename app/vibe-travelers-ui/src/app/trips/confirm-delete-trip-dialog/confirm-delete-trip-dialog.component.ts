import {
  AfterViewChecked,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  ViewChild,
} from '@angular/core';

import { ApiErrorVm, PendingDeleteTripVm } from '../trips.models';
import { isValidTripId } from '../trip-id.util';

@Component({
  selector: 'app-confirm-delete-trip-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './confirm-delete-trip-dialog.component.html',
  styleUrl: './confirm-delete-trip-dialog.component.sass',
})
export class ConfirmDeleteTripDialogComponent implements OnChanges, AfterViewChecked {
  @Input() trip: PendingDeleteTripVm | null = null;
  @Input() isDeleting = false;
  @Input() error: ApiErrorVm | null = null;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  @ViewChild('cancelButton') private readonly cancelButton?: ElementRef<HTMLButtonElement>;

  private elementToRestore: HTMLElement | null = null;
  private shouldFocusDialog = false;

  get canConfirm(): boolean {
    return this.trip !== null && isValidTripId(this.trip.id) && !this.isDeleting;
  }

  ngOnChanges(changes: SimpleChanges): void {
    const tripChange = changes['trip'];

    if (!tripChange) {
      return;
    }

    const previousTrip = tripChange.previousValue as PendingDeleteTripVm | null;
    const currentTrip = tripChange.currentValue as PendingDeleteTripVm | null;

    if (!previousTrip && currentTrip) {
      this.elementToRestore = document.activeElement instanceof HTMLElement ? document.activeElement : null;
      this.shouldFocusDialog = true;
    }

    if (previousTrip && !currentTrip) {
      this.restoreFocus();
    }
  }

  ngAfterViewChecked(): void {
    if (!this.shouldFocusDialog || !this.cancelButton) {
      return;
    }

    this.shouldFocusDialog = false;
    this.cancelButton.nativeElement.focus();
  }

  requestCancel(): void {
    if (this.isDeleting) {
      return;
    }

    this.cancel.emit();
  }

  requestConfirm(): void {
    if (!this.canConfirm) {
      return;
    }

    this.confirm.emit();
  }

  private restoreFocus(): void {
    const elementToRestore = this.elementToRestore;
    this.elementToRestore = null;

    if (!elementToRestore?.isConnected) {
      return;
    }

    queueMicrotask(() => elementToRestore.focus());
  }
}
