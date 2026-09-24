import { copyFile, mkdir } from 'node:fs/promises';
import './import-golden-bell.mjs';
const destination = new URL('../src/content/', import.meta.url);
await mkdir(destination, { recursive: true });
await copyFile(new URL('../../content/wordy-wings/hide-seek/levels.v1.json', import.meta.url), new URL('hide-seek.json', destination));
for (const [source, target] of [['mvp_levels_30.json', 'levels.json'], ['worlds.json', 'worlds.json']]) {
  await copyFile(new URL(`../../InitialDocs/wordy_wings_gdd/data/${source}`, import.meta.url), new URL(target, destination));
}
