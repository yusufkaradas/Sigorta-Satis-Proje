import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

const CARD_QUERY = '(max-width: 900px)';
const SKIPPED_TABLES = '.matrix-table, .sim-table, .kt-keep-table';
const ACTION_CONTROLS = 'button, .detail-button, .offer-button, .policy-button, .action-button, .cp-act';

@Injectable({ providedIn: 'root' })
export class ResponsiveTableService {

  private readonly document = inject(DOCUMENT);

  private started = false;

  start(): void {
    const view = this.document.defaultView;

    if (this.started || !view || typeof MutationObserver === 'undefined') {
      return;
    }

    this.started = true;

    const media = view.matchMedia(CARD_QUERY);

    const update = () => {
      if (media.matches) {
        this.labelTables();
      }
    };

    new MutationObserver(update).observe(this.document.body, { childList: true, subtree: true });

    media.addEventListener('change', update);

    update();
  }

  private labelTables(): void {
    this.document.querySelectorAll<HTMLTableElement>('table').forEach(table => {
      if (table.matches(SKIPPED_TABLES)) {
        return;
      }

      const headers = Array.from(table.querySelectorAll('thead th'))
        .map(th => th.textContent?.replace(/\s+/g, ' ').trim() ?? '');

      if (headers.length === 0) {
        return;
      }

      table.classList.add('kt-cards');

      Array.from(table.tBodies).forEach(body =>
        Array.from(body.rows).forEach(row => this.labelRow(row, headers))
      );
    });
  }

  private labelRow(row: HTMLTableRowElement, headers: string[]): void {
    const cells = Array.from(row.cells);
    const isFullRow = cells.length === 1 && cells[0].colSpan > 1;

    row.classList.toggle('kt-row-full', isFullRow);

    if (isFullRow) {
      return;
    }

    let column = 0;

    cells.forEach(cell => {
      const label = headers[column] ?? '';
      column += cell.colSpan || 1;

      if (label) {
        if (cell.getAttribute('data-label') !== label) {
          cell.setAttribute('data-label', label);
        }
      } else {
        cell.removeAttribute('data-label');
      }

      const isActionCell = (!label || label.toLocaleLowerCase('tr-TR').includes('işlem')) && cell.querySelector(ACTION_CONTROLS) !== null;

      cell.classList.toggle('kt-actions', isActionCell);
    });
  }
}
