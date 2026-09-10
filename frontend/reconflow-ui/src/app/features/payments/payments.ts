import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Customer, Payment, PaymentImportResult } from '../../core/models';

@Component({ selector: 'app-payments', imports: [FormsModule, CurrencyPipe, DatePipe], templateUrl: './payments.html', styleUrl: './payments.scss' })
export class PaymentsComponent implements OnInit {
  readonly payments = signal<Payment[]>([]); readonly customers = signal<Customer[]>([]); readonly loading = signal(true); readonly error = signal(''); readonly success = signal(''); readonly importResult = signal<PaymentImportResult | null>(null); readonly importing = signal(false);
  showPaymentForm = false; search = ''; selectedFile: File | null = null;
  payment = { transactionReference: '', customerId: '', amount: 0, paymentDate: new Date().toISOString().slice(0, 10), description: '' };
  constructor(private readonly api: ApiService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.loading.set(true); forkJoin({ payments: this.api.payments(), customers: this.api.customers() }).pipe(finalize(() => this.loading.set(false))).subscribe({ next: (x) => { this.payments.set(x.payments); this.customers.set(x.customers); }, error: (e) => this.error.set(e.error?.message ?? 'Payments could not be loaded.') }); }
  visible(): Payment[] { const term = this.search.toLowerCase(); return this.payments().filter((x) => !term || `${x.transactionReference} ${x.customerName} ${x.description}`.toLowerCase().includes(term)); }
  savePayment(): void { this.api.createPayment({ ...this.payment, amount: Number(this.payment.amount) }).subscribe({ next: (payment) => { this.success.set('Payment stored. Run reconciliation when ready.'); this.showPaymentForm = false; this.payments.update((rows) => [payment, ...rows]); }, error: (e) => this.error.set(e.error?.message ?? 'Payment could not be created.') }); }
  run(payment: Payment): void { this.api.runReconciliation(payment.id).subscribe({ next: (result) => { this.success.set(`${payment.transactionReference} classified as ${result.matchStatus} (${result.matchScore}%).`); this.load(); }, error: (e) => this.error.set(e.error?.message ?? 'Reconciliation could not run.') }); }
  selectFile(event: Event): void { this.selectedFile = (event.target as HTMLInputElement).files?.[0] ?? null; this.importResult.set(null); }
  upload(): void { if (!this.selectedFile) return; this.importing.set(true); this.error.set(''); this.api.importPayments(this.selectedFile).pipe(finalize(() => this.importing.set(false))).subscribe({ next: (result) => { this.importResult.set(result); this.success.set(`Processed ${result.uploaded} payment rows.`); this.load(); }, error: (e) => this.error.set(e.error?.message ?? 'CSV import failed.') }); }
}
