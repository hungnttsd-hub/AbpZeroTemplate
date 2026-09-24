import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import demos from './src/content/balloon-dart-demos.json';
import campaign from './src/content/levels.json';
import { balloonDartSchema, resolveBalloonDart } from './src/game/balloon-dart/model';
import hideSeek from './src/content/hide-seek.json';
import { parseContent } from './src/game/hide-seek/content';

// Fail during dev startup/build, before invalid content reaches a child.
for (const demo of demos) balloonDartSchema.parse(demo);
parseContent(hideSeek);
for (const level of campaign) if (level.mechanic === 'balloon_pop' || level.mechanic === 'balloon_dart') resolveBalloonDart(level);

export default defineConfig({
  plugins: [react()],
  base: '/wordy-wings/',
  server: { proxy: Object.fromEntries(['/api', '/Account', '/Legal', '/connect', '/Abp', '/libs', '/__bundles'].map(path => [path, { target: process.env.WORDY_API_URL || 'https://localhost:44433', secure: false, changeOrigin: true }])) },
  build: { rollupOptions: { output: { manualChunks: { phaser: ['phaser'] } } } }
});
