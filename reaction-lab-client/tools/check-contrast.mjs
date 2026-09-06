import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const tokenFile = process.argv[2] ?? join(root, 'src', 'app', 'design-system', 'tokens', '_color.scss');

const TEXT_MINIMUM = 4.5;
const UI_MINIMUM = 3;

const SURFACES = ['surface-void', 'surface-base', 'surface-panel', 'surface-raised', 'surface-overlay',
    'surface-hover', 'surface-active'];
const CATEGORIES = ['alkali', 'alkaline-earth', 'transition', 'post-transition', 'metalloid', 'nonmetal', 'halogen',
    'noble', 'lanthanide', 'actinide', 'unknown'];
const STATES = ['state-solid', 'state-liquid', 'state-gas', 'state-aqueous', 'state-plasma'];

const text = (fg, bg, over = []) => ({ fg, bg, over, minimum: TEXT_MINIMUM });
const ui = (fg, bg, over = []) => ({ fg, bg, over, minimum: UI_MINIMUM });

const PAIRS = [
    ...SURFACES.flatMap((surface) => [text('text-primary', surface), text('text-secondary', surface),
    text('text-tertiary', surface)]),
    text('text-primary', 'glass-bg', ['surface-void']),
    text('text-secondary', 'glass-bg', ['surface-void']),
    text('text-tertiary', 'glass-bg', ['surface-void']),
    text('accent-contrast', 'accent'),
    text('accent', 'surface-panel'),
    text('accent', 'surface-raised'),
    text('text-primary', 'accent-subtle', ['glass-bg', 'surface-void']),
    ui('accent', 'accent-subtle', ['glass-bg', 'surface-void']),
    ...['success', 'warning', 'danger', 'info'].map((tone) => text('accent-contrast', tone)),
    ...['success', 'warning', 'danger'].flatMap((tone) => [text(tone, 'surface-panel'), text(tone,
        'surface-raised')]),
    ...CATEGORIES.map((category) => text('cat-ink', `cat-${category}`)),
    ...STATES.flatMap((state) => [text(state, 'surface-panel'), text(state, 'surface-raised')]),
    ...['energy-exo', 'energy-endo'].flatMap((energy) => [text(energy, 'surface-panel'), text(energy,
        'surface-raised')]),
    ui('accent-ring', 'surface-panel', ['surface-panel']),
    ui('accent', 'surface-void'),
    ui('border-strong', 'surface-panel')
];

const light = tokens(block(tokenFile, ':root {'));
const dark = { ...light, ...tokens(block(tokenFile, '@mixin dark {')) };

let failures = 0;
const rows = PAIRS.map((pair) => {
    const inLight = ratio(pair, light);
    const inDark = ratio(pair, dark);
    const ok = inLight >= pair.minimum && inDark >= pair.minimum;

    if (!ok) {
        failures += 1;
    }

    return {
        pair: `${pair.fg} on ${pair.bg}${pair.over.length ? ` over ${pair.over.join(' over ')}` : ''}`,
        light: inLight.toFixed(2),
        dark: inDark.toFixed(2),
        minimum: pair.minimum.toFixed(1),
        status: ok ? 'ok' : 'FAIL'
    };
});

console.table(rows);
console.log(failures === 0 ? `All ${rows.length} token pairs meet their minimum in both themes.` : `${failures} of
    ${rows.length} token pairs fall short.`);
process.exit(failures === 0 ? 0 : 1);

function block(file, opener) {
    const source = readFileSync(file, 'utf8');
    const start = source.indexOf(opener);

    if (start < 0) {
        throw new Error(`${opener} not found in ${file}`);
    }

    return source.slice(start + opener.length, source.indexOf('}', start));
}

function tokens(scss) {
    const declared = {};

    for (const match of scss.matchAll(/--([a-z0-9-]+):\s*([^;]+);/g)) {
        declared[match[1]] = match[2].trim();
    }

    return declared;
}

function ratio(pair, theme) {
    const foreground = resolve(pair.fg, theme);
    const layers = [pair.bg, ...pair.over].map((name) => resolve(name, theme));
    const background = layers.reduceRight((below, layer) => composite(layer, below));
    const fg = composite(foreground, background);
    const l1 = luminance(fg);
    const l2 = luminance(background);

    return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
}

function resolve(name, theme) {
    const value = theme[name];

    if (value === undefined) {
        throw new Error(`Token --${name} is not defined`);
    }

    const alias = value.match(/^var\(--([a-z0-9-]+)\)$/);

    return alias ? resolve(alias[1], theme) : oklch(value);
}

function oklch(value) {
    const match = value.match(/oklch\(\s*([\d.]+)%\s+([\d.]+)\s+([\d.]+)deg(?:\s*\/\s*([\d.]+)%)?\s*\)/);

    if (!match) {
        throw new Error(`Cannot parse ${value}`);
    }

    const [, lightness, chroma, hue, alpha] = match;
    const [r, g, b] = toLinearSrgb(Number(lightness) / 100, Number(chroma), Number(hue));

    return { r: clamp(r), g: clamp(g), b: clamp(b), a: alpha === undefined ? 1 : Number(alpha) / 100 };
}

function toLinearSrgb(l, c, h) {
    const a = c * Math.cos((h * Math.PI) / 180);
    const b = c * Math.sin((h * Math.PI) / 180);
    const l_ = l + 0.3963377774 * a + 0.2158037573 * b;
    const m_ = l - 0.1055613458 * a - 0.0638541728 * b;
    const s_ = l - 0.0894841775 * a - 1.291485548 * b;
    const [l3, m3, s3] = [l_ ** 3, m_ ** 3, s_ ** 3];

    return [
        4.0767416621 * l3 - 3.3077115913 * m3 + 0.2309699292 * s3,
        -1.2684380046 * l3 + 2.6097574011 * m3 - 0.3413193965 * s3,
        -0.0041960863 * l3 - 0.7034186147 * m3 + 1.707614701 * s3
    ];
}

function composite(top, bottom) {
    if (top.a >= 1) {
        return top;
    }

    return {
        r: top.a * top.r + (1 - top.a) * bottom.r,
        g: top.a * top.g + (1 - top.a) * bottom.g,
        b: top.a * top.b + (1 - top.a) * bottom.b,
        a: 1
    };
}

function luminance({ r, g, b }) {
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function clamp(channel) {
    return Math.min(1, Math.max(0, channel));
}