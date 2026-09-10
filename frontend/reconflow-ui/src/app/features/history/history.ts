import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Reconciliation, ReconciliationStatus } from '../../core/models';

@Component({ selector: 'app-history', imports: [FormsModule, CurrencyPipe, DatePipe], templateUrl: './history.html', styleUrl: './history.scss' })
export class HistoryComponent implements OnInit {
  readonly rows = signal<Reconciliation[]>([]); readonly loading = signal(true); readonly error = signal(''); search = ''; status = '';
  constructor(private readonly api: ApiService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.loading.set(true); this.api.reconciliations(this.status ? this.status as ReconciliationStatus : undefined).pipe(finalize(() => this.loading.set(false))).subscribe({ next: (x) => this.rows.set(x), error: (e) => this.error.set(e.error?.message ?? 'History could not be loaded.') }); }
  visible(): Reconciliation[] { const term = this.search.toLowerCase(); return this.rows().filter((x) => !term || `${x.paymentReference} ${x.invoiceNumber} ${x.customerName}`.toLowerCase().includes(term)); }
}
