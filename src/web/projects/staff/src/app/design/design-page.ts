import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChevronDown,
  lucideCircleCheck,
  lucideCircleDot,
  lucideInfo,
  lucideKeyRound,
  lucideLogOut,
  lucidePlus,
  lucideTrash2,
  lucideTriangleAlert,
} from '@ng-icons/lucide';
import { THEMES, type Theme } from '@hirelab/ui';
import { CopyButton } from '@hirelab/ui/copy';
import { HlmBadge } from '@hirelab/ui/helm/badge';
import { HlmButton } from '@hirelab/ui/helm/button';
import { HlmCardImports } from '@hirelab/ui/helm/card';
import { HlmDialogImports } from '@hirelab/ui/helm/dialog';
import { HlmDropdownMenuImports } from '@hirelab/ui/helm/dropdown-menu';
import { HlmInput } from '@hirelab/ui/helm/input';
import { HlmLabel } from '@hirelab/ui/helm/label';
import { HlmTextarea } from '@hirelab/ui/helm/textarea';
import { PresenceIndicator, ReconnectingBanner, type Presence } from '@hirelab/ui/live';
import { MarkdownViewer } from '@hirelab/ui/markdown';
import { CodeDemo } from './code-demo';

@Component({
  selector: 'hl-design-page',
  imports: [
    NgIcon,
    CodeDemo,
    CopyButton,
    HlmBadge,
    HlmButton,
    HlmCardImports,
    HlmDialogImports,
    HlmDropdownMenuImports,
    HlmInput,
    HlmLabel,
    HlmTextarea,
    MarkdownViewer,
    PresenceIndicator,
    ReconnectingBanner,
  ],
  providers: [
    provideIcons({
      lucideChevronDown,
      lucideCircleCheck,
      lucideCircleDot,
      lucideInfo,
      lucideKeyRound,
      lucideLogOut,
      lucidePlus,
      lucideTrash2,
      lucideTriangleAlert,
    }),
  ],
  templateUrl: './design-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DesignPage {
  protected readonly themes: readonly Theme[] = THEMES;

  protected readonly markdownSample = [
    '### Requirements',
    '',
    '- **Three** years of backend work',
    '- Knows `SQL` and [HTTP](https://example.com)',
    '',
    '> Interviewer notes stay with the team.',
  ].join('\n');

  protected readonly presences: readonly Presence[] = [
    { connected: true, connections: 1, lastSeenAt: '2026-01-05T09:00:00Z' },
    { connected: true, connections: 2, lastSeenAt: '2026-01-05T09:02:00Z' },
    { connected: false, connections: 0, lastSeenAt: '2026-01-05T09:40:00Z' },
    { connected: false, connections: 0, lastSeenAt: null },
  ];

  protected readonly buttonVariants = [
    'default',
    'secondary',
    'outline',
    'ghost',
    'destructive',
    'link',
  ] as const;

  protected readonly swatches = [
    { surface: 'bg-background text-foreground', label: 'background' },
    { surface: 'bg-card text-card-foreground', label: 'card' },
    { surface: 'bg-muted text-muted-foreground', label: 'muted' },
    { surface: 'bg-primary text-primary-foreground', label: 'primary' },
    { surface: 'bg-success text-success-foreground', label: 'success' },
    { surface: 'bg-warning text-warning-foreground', label: 'warning' },
    { surface: 'bg-destructive text-destructive-foreground', label: 'destructive' },
  ];
}
