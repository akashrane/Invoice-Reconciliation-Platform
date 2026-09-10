import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Reconciliation } from '../../core/models';

@Component({ selector: 'app-review', imports: [CurrencyPipe, DatePipe], templateUrl: './review.html', styleUrl: './review.scss' })
export class ReviewComponent implements OnInit {
  readonly queue = signal<Reconciliation[]>([]); readonly loading = signal(true); readonly error = signal(''); readonly success = signal(''); readonly working = signal('');
  constructor(private readonly api: ApiService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.loading.set(true); this.api.reviewQueue().pipe(finalize(() => this.loading.set(false))).subscribe({ next: (x) => this.queue.set(x), error: (e) => this.error.set(e.error?.message ?? 'Review queue could not be loaded.') }); }
  decide(item: Reconciliation, approve: boolean): void {
    const verb = approve ? 'approve why this payment should be approved' : 'Explain why this suggestion is being rejected';
    const reason = window.prompt(verb, approve ? 'Verified against remittance advice' : 'Payment belongs to another invoice');
    if (!reason || reason.trim().length < 3) return;
    if (!window.confirm(`${approve ? 'Approve' : 'Reject'} ${item.paymentReference}? This decision will be audited.`)) return;
    this.working.set(item.id);
    const request = approve ? this.api.approve(item.id, reason) : this.api.reject(item.id, reason);
    request.pipe(finalize(() => this.working.set(''))).subscribe({ next: () => { this.success.set(`${item.paymentReference} ${approve ? 'approved' : 'rejected'} and audit entry recorded.`); this.queue.update((rows) => rows.filter((x) => x.id !== item.id)); }, error: (e) => this.error.set(e.error?.message ?? 'Decision could not be saved.') });
  }
  positive(reason: string): boolean { return reason.includes('(+') && !reason.includes('(+0)'); }
}
