import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeSwitcher } from '@hirelab/ui';

@Component({
  selector: 'hl-root',
  imports: [RouterOutlet, ThemeSwitcher],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
