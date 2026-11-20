import type { CodeLineDecoration } from '@hirelab/ui/code';
import type { BadgeVariants } from '@hirelab/ui/helm/badge';
import {
  findingSeverities,
  type ExpectedFinding,
  type FindingSeverity,
} from '../candidate/code-review';

export const severityBadgeVariants: Readonly<Record<FindingSeverity, BadgeVariants['variant']>> = {
  Critical: 'destructive',
  Major: 'default',
  Minor: 'secondary',
  Nit: 'outline',
};

/** The findings, most severe first, then by file and line. */
export function sortFindings(findings: readonly ExpectedFinding[]): ExpectedFinding[] {
  return [...findings].sort(
    (a, b) =>
      findingSeverities.indexOf(a.severity) - findingSeverities.indexOf(b.severity) ||
      a.file.localeCompare(b.file) ||
      a.startLine - b.startLine,
  );
}

export function findingDecorations(
  file: string,
  findings: readonly ExpectedFinding[],
): CodeLineDecoration[] {
  return findings
    .filter((finding) => finding.file === file)
    .map(({ side, startLine, endLine, severity, title }) => ({
      side,
      startLine,
      endLine,
      kind: 'finding',
      hoverMessage: `Expected finding (${severity}): ${title}`,
    }));
}
