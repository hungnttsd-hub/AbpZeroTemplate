import { cp, mkdir } from 'node:fs/promises';
const target = new URL('../../src/WebHoanTien.Web/wwwroot/wordy-wings/', import.meta.url);
await mkdir(target, { recursive: true });
await cp(new URL('../dist/', import.meta.url), target, { recursive: true });
console.log('Wordy Wings is available at /wordy-wings/index.html on the ABP host.');
