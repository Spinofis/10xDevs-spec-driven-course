import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-trip-list-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './trip-list-pagination.component.html',
  styleUrl: './trip-list-pagination.component.sass',
})
export class TripListPaginationComponent {
  @Input() nextCursor: string | null = null;
  @Input() canGoBack = false;
  @Input() isLoading = false;
  @Input() pageIndex = 1;

  @Output() previous = new EventEmitter<void>();
  @Output() next = new EventEmitter<void>();

  get canGoNext(): boolean {
    return Boolean(this.nextCursor) && !this.isLoading;
  }

  get canGoPrevious(): boolean {
    return this.canGoBack && !this.isLoading;
  }
}
