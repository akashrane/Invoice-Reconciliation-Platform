import { computed, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResult } from './models';

const STORAGE_KEY = 'reconflow.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<AuthResult | null>(this.readSession());
  readonly user = this.session.asReadonly();
  readonly isAuthenticated = computed(() => !!this.session()?.token);
  constructor(private readonly http: HttpClient, private readonly router: Router) {}
  login(email: string, password: string): Observable<AuthResult> { return this.http.post<AuthResult>('/api/auth/login', { email, password }).pipe(tap((result) => this.save(result))); }
  register(email: string, password: string): Observable<AuthResult> { return this.http.post<AuthResult>('/api/auth/register', { email, password }).pipe(tap((result) => this.save(result))); }
  token(): string | null { return this.session()?.token ?? null; }
  logout(): void { localStorage.removeItem(STORAGE_KEY); this.session.set(null); void this.router.navigateByUrl('/login'); }
  private save(result: AuthResult): void { localStorage.setItem(STORAGE_KEY, JSON.stringify(result)); this.session.set(result); }
  private readSession(): AuthResult | null { try { return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null') as AuthResult | null; } catch { return null; } }
}
