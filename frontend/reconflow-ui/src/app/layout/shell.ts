import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({ selector: 'app-shell', imports: [RouterOutlet, RouterLink, RouterLinkActive], templateUrl: './shell.html', styleUrl: './shell.scss' })
export class ShellComponent { constructor(readonly auth: AuthService) {} }
