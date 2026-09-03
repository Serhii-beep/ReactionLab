// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const prettier = require('eslint-config-prettier/flat');
const boundaries = /** @type {import('eslint').ESLint.Plugin} */ (require('eslint-plugin-boundaries'));

module.exports = defineConfig([
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
          prefix: ['app', 'rl'],
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: ['app', 'rl'],
          style: 'kebab-case',
        },
      ],
      'max-lines': ['error', { max: 250, skipBlankLines: true, skipComments: true }],
      'max-lines-per-function': ['error', { max: 50, skipBlankLines: true, skipComments: true }],
      complexity: ['error', 10],
      'max-params': ['error', 5],
      'max-depth': ['error', 4]
    },
  },
  {
    files: ['**/*.spec.ts'],
    rules: { 'max-lines': 'off', 'max-lines-per-function': 'off' }
  },
  {
    files: ['src/app/**/*.ts'],
    plugins: { boundaries },
    settings: {
      'import/resolver': { node: { extensions: ['.ts'] } },
      'boundaries/elements': [
        { type: 'design-system', pattern: 'src/app/design-system/**', partialMatch: false },
        { type: 'data', pattern: 'src/app/data/**', partialMatch: false },
        { type: 'state', pattern: 'src/app/state/**', partialMatch: false },
        { type: 'core', pattern: 'src/app/core/**', partialMatch: false },
        { type: 'feature', pattern: 'src/app/features/*/**', partialMatch: false, capture: ['name'] }
      ]
    },
    rules: {
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          policies: [
            { from: [{ element: { type: 'design-system' } }], allow: [{ to: { element: { type: 'design-system' } } }] },
            { from: [{ element: { type: 'data' } }], allow: [{ to: { element: { type: 'data' } } }] },
            { from: [{ element: { type: 'state' } }], allow: [{ to: { element: { type: ['state', 'data'] } } }] },
            { from: [{ element: { type: 'core' } }], allow: [{ to: { element: { type: ['core', 'data', 'design-system'] } } }] },
            {
              from: [{ element: { type: 'feature' } }],
              allow: [
                { to: { element: { type: ['data', 'state', 'core', 'design-system'] } } },
                { to: { element: { type: 'feature', captured: { name: '{{from.name}}' } } } }
              ]
            }
          ]
        }
      ]
    }
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
  prettier
]);
