import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Customer, Invoice, Payment, PaymentImportResult, Reconciliation, ReconciliationStatus } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient) {}
  customers(search = '') { return this.http.get<Customer[]>('/api/customers', { params: search ? { search } : {} }); }
  createCustomer(body: { name: string; email: string; accountReference: string }) { return this.http.post<Customer>('/api/customers', body); }
  invoices(filters: Record<string, string> = {}) { return this.http.get<Invoice[]>('/api/invoices', { params: filters }); }
  createInvoice(body: object) { return this.http.post<Invoice>('/api/invoices', body); }
  payments(filters: Record<string, string> = {}) { return this.http.get<Payment[]>('/api/payments', { params: filters }); }
  createPayment(body: object) { return this.http.post<Payment>('/api/payments', body); }
  importPayments(file: File) { const form = new FormData(); form.append('file', file); return this.http.post<PaymentImportResult>('/api/payments/import', form); }
  runReconciliation(paymentId: string) { return this.http.post<Reconciliation>(`/api/reconciliation/run/${paymentId}`, {}); }
  reconciliations(status?: ReconciliationStatus) { const params = status ? new HttpParams().set('status', status) : undefined; return this.http.get<Reconciliation[]>('/api/reconciliation', { params }); }
  reviewQueue() { return this.http.get<Reconciliation[]>('/api/reconciliation/review'); }
  approve(id: string, reason: string) { return this.http.post<Reconciliation>(`/api/reconciliation/${id}/approve`, { reason }); }
  reject(id: string, reason: string) { return this.http.post<Reconciliation>(`/api/reconciliation/${id}/reject`, { reason }); }
}
