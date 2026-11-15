import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { parse as parseScss } from 'postcss-scss';
import { clampRgb, converter, parse as parseColor } from 'culori';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const tokenFile = join(root, 'src', 'app', 'design-system', 'tokens', '_color.scss');
const rustFile = join(root, '..', 'reaction-lab-desktop', 'render', 'src', 'theme_colors.rs');
const toLinear = converter('lrgb');

const stylesheet = parseScss(readFileSync(tokenFile, 'utf8'), { from: tokenFile });
const isRoot = (node) => node.type === 'rule' && node.selector === ':root';
const isDarkMixin = (node) => node.type === 'atrule' && node.name === 'mixin' && node.params === 'dark';
const light = declarations(':root', isRoot);
const dark = { ...light, ...declarations('@mixin dark', isDarkMixin) };
const names = Object.keys(dark);

writeFileSync(rustFile, [
    '// Generated from _color.scss by `npm run desktop-tokens`. Edit the tokens, not this file.',
    '',
    'use wgpu::Color;',
    '',
    'pub struct ThemeColors {',
    ...names.map((name) => `    pub ${field(name)}: Color,`),
    '}',
    '',
    'impl ThemeColors {',
    ...theme('LIGHT', 'light', light),
    '',
    ...theme('DARK', 'dark', dark),
    '}',
    '',
    'const fn linear_color(red: f64, green: f64, blue: f64, alpha: f64) -> Color {',
    '    Color { r: red, g: green, b: blue, a: alpha }',
    '}',
    '',
].join('\n'));

console.log(`Wrote ${names.length} colors for each theme to ${rustFile}.`);

function declarations(blockName, matches) {
    const block = stylesheet.nodes.find(matches);

    if (!block) {
        throw new Error(`The ${blockName} block is missing from ${tokenFile}.`);
    }

    return Object.fromEntries(block.nodes
        .filter((node) => node.type === 'decl' && node.prop.startsWith('--'))
        .map((node) => [node.prop.slice(2), node.value.trim()]));
}

function theme(constant, themeName, tokens) {
    return [
        `    pub const ${constant}: Self = Self {`,
        ...names.map((name) => `        ${field(name)}: ${linearColor(name, themeName, tokens)},`),
        '    };',
    ];
}

function linearColor(name, themeName, tokens) {
    const value = tokens[name];

    if (value === undefined) {
        throw new Error(`Token --${name} has no value in the ${themeName} theme.`);
    }

    const alias = value.match(/^var\(--([a-z0-9-]+)\)$/);

    if (alias) {
        return linearColor(alias[1], themeName, tokens);
    }

    const color = parseColor(value);

    if (!color) {
        throw new Error(`Token --${name} in the ${themeName} theme is not a color: ${value}.`);
    }

    const linear = toLinear(clampRgb(color));

    return `linear_color(${[linear.r, linear.g, linear.b, linear.alpha ?? 1].map(literal).join(', ')})`;
}

function field(name) {
    return name.replaceAll('-', '_');
}

function literal(channel) {
    const text = String(Math.round(channel * 1e6) / 1e6);

    return text.includes('.') ? text : `${text}.0`;
}
