import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { forkJoin, finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Invoice, Payment, Reconciliation } from '../../core/models';

@Component({ selector: 'app-dashboard', imports: [CurrencyPipe, DatePipe], templateUrl: './dashboard.html', styleUrl: './dashboard.scss' })
export class DashboardComponent implements OnInit {
  readonly invoices = signal<Invoice[]>([]); readonly payments = signal<Payment[]>([]); readonly history = signal<Reconciliation[]>([]);
  readonly loading = signal(true); readonly error = signal('');
  constructor(private readonly api: ApiService) {}
  ngOnInit(): void { this.refresh(); }
  refresh(): void {
    this.loading.set(true); this.error.set('');
    forkJoin({ invoices: this.api.invoices(), payments: this.api.payments(), history: this.api.reconciliations() })
      .pipe(finalize(() => this.loading.set(false))).subscribe({
        next: (data) => { this.invoices.set(data.invoices); this.payments.set(data.payments); this.history.set(data.history); },
        error: (error) => this.error.set(error.error?.message ?? 'Dashboard data could not be loaded.'),
      });
  }
  count(status: string): number { return this.history().filter((x) => x.matchStatus === status).length; }
  openInvoices(): number { return this.invoices().filter((x) => x.status !== 'Paid').length; }
  paymentsToday(): number { const today = new Date().toISOString().slice(0, 10); return this.payments().filter((x) => x.paymentDate === today).length; }
  rate(): number { const total = this.history().length; return total ? Math.round((this.count('Matched') / total) * 100) : 0; }
  invoiceCount(status: string): number { return this.invoices().filter((x) => x.status === status).length; }
  outstandingTotal(): number { return this.invoices().filter((x) => x.status !== 'Paid').reduce((sum, x) => sum + x.amount, 0); }
  openShare(): number { const total = this.invoices().length; return total ? (this.openInvoices() / total) * 100 : 0; }
}
