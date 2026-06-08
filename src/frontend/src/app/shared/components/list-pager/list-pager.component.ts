import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-list-pager',
  template: `
    @if (total() > 0) {
      <nav class="list-pager" aria-label="Paginacao da lista">
        <span>{{ startItem() }}-{{ endItem() }} de {{ total() }}</span>
        <div>
          <button type="button" class="ghost-button" [disabled]="page() <= 1" (click)="previous()">
            Anterior
          </button>
          <strong>{{ page() }} / {{ totalPages() }}</strong>
          <button type="button" class="ghost-button" [disabled]="page() >= totalPages()" (click)="next()">
            Proxima
          </button>
        </div>
      </nav>
    }
  `
})
export class ListPagerComponent {
  readonly total = input.required<number>();
  readonly page = input.required<number>();
  readonly pageSize = input(10);
  readonly pageChange = output<number>();

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  readonly startItem = computed(() => Math.min(this.total(), ((this.page() - 1) * this.pageSize()) + 1));
  readonly endItem = computed(() => Math.min(this.total(), this.page() * this.pageSize()));

  previous(): void {
    this.pageChange.emit(Math.max(1, this.page() - 1));
  }

  next(): void {
    this.pageChange.emit(Math.min(this.totalPages(), this.page() + 1));
  }
}
