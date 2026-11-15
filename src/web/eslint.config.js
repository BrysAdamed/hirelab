// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const boundaries = require('eslint-plugin-boundaries');

const element = (type, pattern) => ({ type, pattern, partialMatch: false });

const staffOnlyLibs = ['api-client-staff', 'tasks-staff'];
const sharedLibs = ['ui', 'core', 'api-client-candidate', 'tasks-candidate'];

module.exports = defineConfig([
  {
    ignores: ['dist/**', 'out-tsc/**', '.angular/**', 'coverage/**'],
  },
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'hl',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'hl',
          style: 'kebab-case',
        },
      ],
    },
  },
  {
    files: ['projects/**/*.ts'],
    plugins: { boundaries },
    settings: {
      'boundaries/root-path': __dirname,
      'import/resolver': { typescript: { project: './tsconfig.json' } },
      'boundaries/elements': [
        element('app-staff', 'projects/staff'),
        element('app-candidate', 'projects/candidate'),
        element('api-client-staff', 'projects/api-client/src/staff'),
        element('api-client-candidate', 'projects/api-client/src/candidate'),
        element('tasks-candidate', 'projects/tasks/src/*/candidate'),
        element('tasks-staff', 'projects/tasks/src/*/review'),
        element('tasks-staff', 'projects/tasks/src/*/editor'),
        element('ui', 'projects/ui'),
        element('core', 'projects/core'),
      ],
    },
    rules: {
      'boundaries/no-unknown-files': 'error',
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          policies: [
            {
              from: { element: { type: 'app-staff' } },
              allow: { to: { element: { types: ['app-staff', ...sharedLibs, ...staffOnlyLibs] } } },
            },
            {
              from: { element: { type: 'app-candidate' } },
              allow: { to: { element: { types: ['app-candidate', ...sharedLibs] } } },
            },
            {
              from: { element: { types: sharedLibs } },
              allow: { to: { element: { types: sharedLibs } } },
            },
            {
              from: { element: { types: staffOnlyLibs } },
              allow: { to: { element: { types: [...sharedLibs, ...staffOnlyLibs] } } },
            },
          ],
        },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
]);
