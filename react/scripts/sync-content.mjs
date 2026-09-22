import { copyFile, mkdir } from 'node:fs/promises';
import './import-golden-bell.mjs';
const destination = new URL('../src/content/', import.meta.url);
await mkdir(destination, { recursive: true });
for (const [source, target] of [['mvp_levels_30.json', 'levels.json'], ['worlds.json', 'worlds.json']]) {
  await copyFile(new URL(`../../InitialDocs/wordy_wings_gdd/data/${source}`, import.meta.url), new URL(target, destination));
}
