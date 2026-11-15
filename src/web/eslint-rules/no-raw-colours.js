// @ts-check
const palette =
  'slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose|mauve|olive|mist|taupe';
const utilities =
  'bg|text|border|ring|outline|fill|stroke|from|via|to|decoration|divide|shadow|accent|caret|placeholder';

const patterns = [
  {
    kind: 'hex',
    regex: /(?<![\w&#])#(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{3,4})(?![\w-])/g,
  },
  { kind: 'function', regex: /\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color)\(/g },
  {
    kind: 'palette',
    regex: new RegExp(
      `\\b(?:${utilities})(?:-[a-z]+)?-(?:(?:${palette})-\\d{2,3}|black|white)(?![\\w-])`,
      'g',
    ),
  },
];

const templateReference = { before: /\s$/, after: /^[\s/>=]|^$/ };

/** @type {import('eslint').Rule.RuleModule} */
module.exports = {
  meta: {
    type: 'problem',
    docs: {
      description:
        'Forbid raw colour values (hex, colour functions, named colours, Tailwind palette classes); use design tokens.',
    },
    schema: [],
    messages: {
      rawColour:
        "Raw colour '{{value}}'. Use a design token (for example bg-primary, text-muted-foreground or var(--border)).",
    },
  },
  create(context) {
    const sourceCode = context.sourceCode;
    const filename = context.filename;

    /**
     * @param {string} text
     * @param {number} offset
     * @param {{ skipTemplateReferences?: boolean }} [options]
     */
    function scan(text, offset, options = {}) {
      for (const { kind, regex } of patterns) {
        for (const match of text.matchAll(regex)) {
          const index = match.index ?? 0;
          if (
            kind === 'hex' &&
            options.skipTemplateReferences &&
            templateReference.before.test(text.slice(0, index)) &&
            templateReference.after.test(text.slice(index + match[0].length))
          ) {
            continue;
          }
          report(offset + index, offset + index + match[0].length, match[0]);
        }
      }
    }

    /**
     * @param {number} start
     * @param {number} end
     * @param {string} value
     */
    function report(start, end, value) {
      context.report({
        loc: { start: sourceCode.getLocFromIndex(start), end: sourceCode.getLocFromIndex(end) },
        messageId: 'rawColour',
        data: { value },
      });
    }

    const inlineTemplate = /\.ts[\\/]/.test(filename);
    if (filename.endsWith('.html')) {
      if (inlineTemplate) {
        return {};
      }
      return {
        Program() {
          scan(sourceCode.text, 0, { skipTemplateReferences: true });
        },
      };
    }

    if (filename.endsWith('.css')) {
      return {
        StyleSheet() {
          const withoutComments = sourceCode.text.replace(/\/\*[\s\S]*?\*\//g, (comment) =>
            ' '.repeat(comment.length),
          );
          scan(withoutComments, 0);
        },
        /** @param {any} node */
        'Declaration Identifier'(node) {
          const lexer = /** @type {any} */ (sourceCode).lexer;
          if (lexer.matchType('named-color', node.name).matched) {
            context.report({ node, messageId: 'rawColour', data: { value: node.name } });
          }
        },
      };
    }

    return {
      /** @param {import('estree').Literal} node */
      Literal(node) {
        if (typeof node.value === 'string' && node.range) {
          scan(sourceCode.getText(node), node.range[0], { skipTemplateReferences: true });
        }
      },
      /** @param {import('estree').TemplateElement} node */
      TemplateElement(node) {
        if (node.range) {
          scan(sourceCode.getText(node), node.range[0], { skipTemplateReferences: true });
        }
      },
    };
  },
};
