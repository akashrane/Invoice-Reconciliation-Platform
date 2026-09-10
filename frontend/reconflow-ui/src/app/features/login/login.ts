import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent {
  email = 'analyst@reconflow.dev';
  password = 'ReconFlow!2026';
  readonly loading = signal(false);
  readonly error = signal('');

  constructor(private readonly auth: AuthService, private readonly router: Router) {
    if (auth.isAuthenticated()) void router.navigateByUrl('/dashboard');
  }

  submit(): void {
    this.error.set('');
    this.loading.set(true);
    this.auth.login(this.email, this.password).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: () => void this.router.navigateByUrl('/dashboard'),
      error: (error) => this.error.set(error.error?.message ?? 'Unable to sign in. Please try again.'),
    });
  }
}
