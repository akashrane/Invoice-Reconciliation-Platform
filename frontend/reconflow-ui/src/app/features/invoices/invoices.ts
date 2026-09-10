import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Customer, Invoice } from '../../core/models';

@Component({ selector: 'app-invoices', imports: [FormsModule, CurrencyPipe, DatePipe], templateUrl: './invoices.html', styleUrl: './invoices.scss' })
export class InvoicesComponent implements OnInit {
  readonly invoices = signal<Invoice[]>([]); readonly customers = signal<Customer[]>([]); readonly loading = signal(true); readonly error = signal(''); readonly success = signal('');
  showInvoiceForm = false; showCustomerForm = false; search = ''; status = '';
  invoice = { invoiceNumber: '', customerId: '', amount: 0, issueDate: new Date().toISOString().slice(0, 10), dueDate: '', status: 'Open' };
  customer = { name: '', email: '', accountReference: '' };
  constructor(private readonly api: ApiService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.loading.set(true); forkJoin({ invoices: this.api.invoices(), customers: this.api.customers() }).pipe(finalize(() => this.loading.set(false))).subscribe({ next: (x) => { this.invoices.set(x.invoices); this.customers.set(x.customers); }, error: (e) => this.error.set(e.error?.message ?? 'Invoices could not be loaded.') }); }
  visible(): Invoice[] { const term = this.search.toLowerCase(); return this.invoices().filter((x) => (!this.status || x.status === this.status) && (!term || `${x.invoiceNumber} ${x.customerName}`.toLowerCase().includes(term))); }
  saveInvoice(): void { this.error.set(''); this.api.createInvoice({ ...this.invoice, amount: Number(this.invoice.amount) }).subscribe({ next: () => { this.success.set('Invoice created successfully.'); this.showInvoiceForm = false; this.invoice = { invoiceNumber: '', customerId: '', amount: 0, issueDate: new Date().toISOString().slice(0, 10), dueDate: '', status: 'Open' }; this.load(); }, error: (e) => this.error.set(e.error?.message ?? 'Invoice could not be created.') }); }
  saveCustomer(): void { this.error.set(''); this.api.createCustomer(this.customer).subscribe({ next: (customer) => { this.success.set('Customer created successfully.'); this.customers.update((rows) => [...rows, customer]); this.invoice.customerId = customer.id; this.customer = { name: '', email: '', accountReference: '' }; this.showCustomerForm = false; }, error: (e) => this.error.set(e.error?.message ?? 'Customer could not be created.') }); }
}
