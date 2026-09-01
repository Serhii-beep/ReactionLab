import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { ToastHost } from './core/notifications/toast-host';

@Component({
  imports: [RouterOutlet, TranslocoDirective, ToastHost],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {}
