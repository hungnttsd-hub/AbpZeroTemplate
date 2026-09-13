import { readdir, mkdir, readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { transform } from 'esbuild';
import sharp from 'sharp';

const root = fileURLToPath(new URL('../wwwroot/', import.meta.url));
const output = path.join(root, 'optimized');
await mkdir(output, { recursive: true });
for (const name of await readdir(root)) {
  const extension = path.extname(name);
  if (!['.css', '.js'].includes(extension) || name === 'service-worker.js') continue;
  const source = await readFile(path.join(root, name), 'utf8');
  const result = await transform(source, {
    loader: extension.slice(1), minify: true, target: ['es2020'], legalComments: 'inline'
  });
  // Keep URLs relative to wwwroot, even though minified CSS lives in /optimized.
  const code = extension === '.css' ? result.code.replace(/url\((['"]?)(?!\/|data:|https?:|#)([^)'"\s]+)\1\)/g,
    (_, quote, url) => `url(${quote}/${url}${quote})`) : result.code;
  await writeFile(path.join(output, name), code);
}
await sharp(path.join(root, 'catback/hero-approved-reference.png'))
  .webp({ quality: 88 }).toFile(path.join(output, 'hero.webp'));
await sharp(path.join(root, 'catback-mascot-reference.png'))
  .resize({ width: 480, withoutEnlargement: true }).webp({ quality: 88 })
  .toFile(path.join(output, 'mascot.webp'));
console.log('Built minified CSS/JS and optimized CatBack images.');
