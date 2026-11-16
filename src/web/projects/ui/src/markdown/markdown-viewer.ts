import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import DOMPurify from 'dompurify';
import { marked } from 'marked';

/**
 * Renders user-authored Markdown (GitHub flavoured) as HTML. The HTML is sanitised with DOMPurify, so scripts, event
 * handlers and `javascript:` links never reach the page. All user-authored Markdown goes through this component.
 */
@Component({
  selector: 'hl-markdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class:
      'block min-w-0 break-words [&_a]:text-accent-text [&_a]:underline [&_blockquote]:border-l-2 [&_blockquote]:pl-3 [&_blockquote]:text-muted-foreground [&_code]:font-mono [&_code]:text-sm [&_h1]:text-lg [&_h1]:font-semibold [&_h2]:text-base [&_h2]:font-semibold [&_h3]:font-semibold [&_hr]:border-border [&_li]:my-0.5 [&_ol]:list-decimal [&_ol]:pl-5 [&_pre]:overflow-x-auto [&_pre]:rounded-md [&_pre]:bg-muted [&_pre]:p-2 [&_table]:border-collapse [&_td]:border [&_td]:px-2 [&_th]:border [&_th]:px-2 [&_ul]:list-disc [&_ul]:pl-5 [&>*+*]:mt-2',
    '[innerHTML]': 'html()',
  },
  template: '',
})
export class MarkdownViewer {
  private readonly sanitizer = inject(DomSanitizer);

  readonly markdown = input.required<string>();

  protected readonly html = computed<SafeHtml>(() => {
    const unsafe = marked.parse(this.markdown(), { async: false, gfm: true });
    return this.sanitizer.bypassSecurityTrustHtml(DOMPurify.sanitize(unsafe));
  });
}
