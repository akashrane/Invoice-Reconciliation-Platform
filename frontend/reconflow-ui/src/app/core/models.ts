export type InvoiceStatus = 'Open' | 'Paid' | 'Overdue';
export type ReconciliationStatus = 'Matched' | 'Review' | 'Unmatched' | 'Rejected';
export interface AuthResult { userId: string; email: string; role: 'Analyst' | 'Admin'; token: string; }
export interface Customer { id: string; name: string; email: string; accountReference: string; createdAt: string; }
export interface Invoice { id: string; invoiceNumber: string; customerId: string; customerName: string; amount: number; issueDate: string; dueDate: string; status: InvoiceStatus; createdAt: string; }
export interface Payment { id: string; transactionReference: string; customerId: string; customerName: string; amount: number; paymentDate: string; description: string; createdAt: string; isReconciled: boolean; }
export interface AuditEntry { id: string; user: string; action: string; previousStatus: ReconciliationStatus; newStatus: ReconciliationStatus; reason: string; createdAt: string; }
export interface Reconciliation { id: string; paymentId: string; paymentReference: string; invoiceId: string | null; invoiceNumber: string | null; customerName: string; amount: number; matchScore: number; matchStatus: ReconciliationStatus; reasons: string[]; reviewedBy: string | null; createdAt: string; reviewedAt: string | null; auditTrail: AuditEntry[]; }
export interface PaymentImportResult { uploaded: number; matched: number; review: number; unmatched: number; failed: number; errors: { row: number; reference: string | null; message: string }[]; }
export interface ApiError { code?: string; message?: string; }
