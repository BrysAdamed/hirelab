import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const require = createRequire(import.meta.url);

const themeFile = join(root, 'projects/ui/src/styles/theme.css');
const paletteFile = join(dirname(require.resolve('tailwindcss/package.json')), 'theme.css');

const themes = ['light', 'dark', 'hc-light', 'hc-dark'];

const AA = 4.5;
const AAA = 7;
const NON_TEXT = 3;

const textPairs = [
  ['foreground', 'background'],
  ['foreground', 'muted'],
  ['card-foreground', 'card'],
  ['popover-foreground', 'popover'],
  ['muted-foreground', 'background'],
  ['muted-foreground', 'muted'],
  ['muted-foreground', 'card'],
  ['muted-foreground', 'popover'],
  ['secondary-foreground', 'secondary'],
  ['accent-foreground', 'accent'],
  ['primary-foreground', 'primary'],
  ['accent-text', 'background'],
  ['accent-text', 'card'],
  ['accent-text', 'popover'],
  ['success', 'background'],
  ['success-foreground', 'success'],
  ['warning-foreground', 'warning'],
  ['destructive', 'background'],
  ['destructive', 'popover'],
  ['destructive-foreground', 'destructive'],
].map(([foreground, background]) => ({ foreground, background, min: { normal: AA, hc: AAA } }));

const nonTextPairs = [
  { foreground: 'ring', background: 'background', min: { normal: NON_TEXT, hc: NON_TEXT } },
  { foreground: 'input', background: 'background', min: { normal: 0, hc: NON_TEXT } },
  { foreground: 'border', background: 'background', min: { normal: 0, hc: NON_TEXT } },
];

const palette = parseDeclarations(readFileSync(paletteFile, 'utf8'));
const themeCss = readFileSync(themeFile, 'utf8');

let failures = 0;
for (const theme of themes) {
  const tokens = parseDeclarations(themeBlock(themeCss, theme));
  const level = theme.startsWith('hc-') ? 'hc' : 'normal';
  console.log(`\n${theme}`);
  for (const pair of [...textPairs, ...nonTextPairs]) {
    const required = pair.min[level];
    if (required === 0) {
      continue;
    }
    const ratio = contrast(
      resolveToken(pair.foreground, tokens),
      resolveToken(pair.background, tokens),
    );
    const passed = ratio >= required;
    if (!passed) {
      failures++;
    }
    const label = `${pair.foreground} on ${pair.background}`;
    console.log(
      `  ${passed ? 'ok  ' : 'FAIL'} ${label.padEnd(40)} ${ratio.toFixed(2).padStart(6)} (min ${required})`,
    );
  }
}

if (failures > 0) {
  console.error(`\n${failures} token pair(s) below the required contrast.`);
  process.exit(1);
}
console.log('\nAll token pairs meet the required contrast.');

function themeBlock(css, theme) {
  const blocks = css.matchAll(/([^{}]+)\{([^{}]*)\}/g);
  for (const [, selectors, body] of blocks) {
    const list = selectors.split(',').map((selector) => selector.trim());
    if (list.includes(`.${theme}`)) {
      return body;
    }
  }
  throw new Error(`No token block for .${theme} in ${themeFile}`);
}

function parseDeclarations(css) {
  const declarations = new Map();
  for (const [, name, value] of css.matchAll(/(--[\w-]+)\s*:\s*([^;]+);/g)) {
    declarations.set(name, value.trim());
  }
  return declarations;
}

function resolveToken(name, tokens) {
  const value = tokens.get(`--${name}`);
  if (!value) {
    throw new Error(`Token --${name} is not defined`);
  }
  return parseColour(value);
}

function parseColour(value) {
  const reference = /^var\((--[\w-]+)\)$/.exec(value);
  if (reference) {
    const resolved = palette.get(reference[1]);
    if (!resolved) {
      throw new Error(`Unknown palette colour ${reference[1]}`);
    }
    return parseColour(resolved);
  }
  const hex = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.exec(value);
  if (hex) {
    const digits = hex[1].length === 3 ? [...hex[1]].map((d) => d + d).join('') : hex[1];
    return [0, 2, 4].map((i) => parseInt(digits.slice(i, i + 2), 16) / 255);
  }
  const oklch = /^oklch\(\s*([\d.]+)(%?)\s+([\d.]+)\s+([\d.]+|none)\s*\)$/.exec(value);
  if (oklch) {
    const lightness = Number(oklch[1]) / (oklch[2] ? 100 : 1);
    return oklchToSrgb(lightness, Number(oklch[3]), oklch[4] === 'none' ? 0 : Number(oklch[4]));
  }
  throw new Error(`Unsupported colour value: ${value}`);
}

function oklchToSrgb(lightness, chroma, hue) {
  const radians = (hue * Math.PI) / 180;
  const a = chroma * Math.cos(radians);
  const b = chroma * Math.sin(radians);
  const l = (lightness + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m = (lightness - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s = (lightness - 0.0894841775 * a - 1.291485548 * b) ** 3;
  const linear = [
    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
  ];
  return linear.map((channel) => {
    const clamped = Math.min(1, Math.max(0, channel));
    return clamped <= 0.0031308 ? 12.92 * clamped : 1.055 * clamped ** (1 / 2.4) - 0.055;
  });
}

function luminance(rgb) {
  const [r, g, b] = rgb.map((channel) =>
    channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4,
  );
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrast(foreground, background) {
  const [lighter, darker] = [luminance(foreground), luminance(background)].sort((x, y) => y - x);
  return (lighter + 0.05) / (darker + 0.05);
}
