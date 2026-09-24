const { test } = require('node:test');
const assert = require('node:assert/strict');
global.crypto ??= require('node:crypto').webcrypto;
const { parseContent, materializeLevel, swipeDistance } = require('../.hide-seek-tests/src/game/hide-seek/content.js');
const { HideSeekController, newSession } = require('../.hide-seek-tests/src/game/hide-seek/controller.js');
const { HideSeekStore } = require('../.hide-seek-tests/src/game/hide-seek/store.js');
const { forestLayout } = require('../.hide-seek-tests/src/game/hide-seek/layout.js');
const bank = parseContent(require('../../content/wordy-wings/hide-seek/levels.v1.json'));
const base = bank.levels[0];
function make(level = base, disk = async () => {}) { return new HideSeekController(newSession(level, bank.entities, 'child'), level, bank.entities, disk); }
function reveal(c, entity) {
  const spot = c.level.spots.find(s => s.entityId === entity); c.selectTool(spot.allowedTools[0]);
  assert.equal(c.reveal(spot.id), true); const id = c.state.pending.id; c.finish(id); return id;
}

test('shared audio fallback reports missing or failed voice without blocking the question', async () => {
  const ts = require('typescript');
  const source = require('node:fs').readFileSync(require('node:path').join(__dirname, '../src/game/services.ts'), 'utf8');
  const js = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
  const { AudioService } = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
  const saved = Object.fromEntries(['window', 'speechSynthesis', 'SpeechSynthesisUtterance'].map(key => [key, Object.getOwnPropertyDescriptor(global, key)]));
  let errors = 0, cancelled = 0;
  try {
    global.window = {};
    const audio = new AudioService({ hasRemoteAssets: false }, false, () => errors++);
    await audio.speak('Find a cat.'); assert.equal(errors, 1);
    global.speechSynthesis = { cancel: () => cancelled++, getVoices: () => [], speak: utterance => utterance.onerror() };
    global.SpeechSynthesisUtterance = class { constructor(text) { this.text = text; } };
    global.window = { speechSynthesis: global.speechSynthesis };
    const c = make(); reveal(c, 'cat');
    await audio.speak(c.target.question); assert.equal(errors, 2);
    assert.equal(c.state.phase, 'asking'); c.answer(true); c.continue(); assert.equal(c.state.phase, 'completed');
    audio.stopAll(); assert.ok(cancelled > 0);
    audio.muted = true; await audio.speak('Find a cat.'); assert.equal(errors, 2);
  } finally { for (const [key, descriptor] of Object.entries(saved)) { if (descriptor) Object.defineProperty(global, key, descriptor); else delete global[key]; } }
});
test('six authored levels and eight entities with complete grammar', () => {
  assert.equal(bank.levels.length, 6); assert.equal(bank.entities.length, 8);
  assert.equal(bank.entities.find(e => e.id === 'scissors').question, 'Are these scissors?');
  assert.equal(bank.entities.find(e => e.id === 'apple').quest, 'Find an apple.');
  assert.equal(bank.entities.find(e => e.id === 'orange').question, 'Is this an orange?');
});
test('all six levels complete through the same controller', () => {
  for (const level of bank.levels) { const c = make(level); reveal(c, level.targetEntityId); c.answer(true); c.continue(); assert.equal(c.state.phase, 'completed'); assert.equal(c.state.stars, 3); }
});
test('seed shuffle is deterministic, varies and never drops or duplicates target', () => {
  assert.deepEqual(materializeLevel(base, 731).spots.map(s => s.entityId), ['cat', 'dog', 'scissors', 'tiger']);
  const mappings = new Set();
  for (let seed = 1; seed <= 120; seed++) {
    const level = materializeLevel(base, seed); assert.deepEqual(level, materializeLevel(base, seed));
    assert.equal(level.spots.filter(s => s.entityId === level.targetEntityId).length, 1);
    assert.deepEqual(level.spots.map(s => s.entityId).sort(), base.spots.map(s => s.entityId).sort());
    mappings.add(level.spots.map(s => s.entityId).join(','));
  }
  assert.ok(mappings.size > 10);
});
test('invalid content is rejected before enabling interactions', () => {
  for (const change of [x => x.levels[0].spots[0].allowedTools = [], x => x.levels[0].spots.push(x.levels[0].spots[0]), x => x.levels[0].spots[0].entityId = 'missing', x => x.levels[0].extra = true]) {
    const value = structuredClone(bank); change(value); assert.throws(() => parseContent(value));
  }
});
test('a swipe measures displacement from origin, not accumulated path length', () => {
  for (let i = 0; i < 100; i++) assert.equal(swipeDistance(100, i % 2 ? 112 : 88, 280).complete, false);
  assert.equal(swipeDistance(100, 196, 280).complete, true); assert.equal(swipeDistance(100, 4, 280).complete, true);
});
test('atomic double tap accepts one event and one penalty', () => {
  const c = make(); reveal(c, 'dog'); assert.equal(c.answer(true, 'one'), true); assert.equal(c.answer(true, 'two'), false);
  assert.equal(c.state.stars, 2); assert.equal(c.state.attempts.length, 1);
});
test('0-star round completes, repeated assisted answers cost no more', () => {
  const c = make(); for (const id of ['dog', 'tiger', 'scissors']) { reveal(c, id); c.answer(true); c.continue(); }
  assert.equal(c.state.stars, 0); reveal(c, 'cat'); c.answer(false); c.continue();
  for (let i = 0; i < 3; i++) { c.answer(false); c.continue(); assert.equal(c.state.stars, 0); }
  c.answer(true); c.continue(); assert.equal(c.state.phase, 'completed'); assert.equal(c.state.attempts.at(-1).assisted, true);
});
test('checkpoint in feedback retains penalty and mapping across twenty controller restarts', () => {
  let c = make(); reveal(c, 'cat'); c.answer(false); const mapping = c.level;
  for (let i = 0; i < 20; i++) { c = new HideSeekController(structuredClone(c.session), base, bank.entities, async () => {}); assert.deepEqual(c.level, mapping); assert.equal(c.state.stars, 2); }
  c.continue(); c.answer(true); c.continue(); assert.equal(c.state.stars, 2); assert.equal(c.state.phase, 'completed');
});
test('pause blocks reveal callbacks and answers, and excludes active time', () => {
  const c = make(); const spot = c.level.spots[0]; c.selectTool(spot.allowedTools[0]); c.reveal(spot.id);
  const id = c.state.pending.id; c.pause(true); assert.equal(c.finish(id), false); c.tick(200); assert.equal(c.session.activeDurationMs, 0);
  c.pause(false); assert.equal(c.finish(id), true); c.pause(true); assert.equal(c.answer(true), false); c.pause(false); c.tick(100); assert.equal(c.session.activeDurationMs, 100);
});
test('all spot hitboxes stay in the gameplay area at required landscape sizes', () => {
  for (const [w, h] of [[1600,900],[1280,720],[1024,768],[844,390],[667,375]]) for (const level of bank.levels) {
    const layout = forestLayout(w,h,level);
    for (const s of layout.spots) { assert.ok(s.width >= 56); assert.ok(s.x >= 0 && s.x + s.width <= w); assert.ok(s.y >= 68 && s.y + s.height <= h - 65); }
  }
});
function memory() { const map = new Map(); return { get: async k => structuredClone(map.get(k)), set: async (k,v) => { await new Promise(r => setTimeout(r, 1)); map.set(k,structuredClone(v)); }, remove: async k => map.delete(k), entries: async () => [] }; }
test('durable completion is counted once, including zero-star results', async () => {
  const store = new HideSeekStore({ owner: 'device', local: true }, 'child', bank, memory()); await store.load();
  const c = make(base, store.checkpoint);
  for (const id of ['dog', 'tiger', 'scissors']) { reveal(c,id); c.answer(true); c.continue(); }
  reveal(c, 'cat'); c.answer(true); c.continue(); await c.checkpoint(); await c.checkpoint();
  assert.deepEqual(store.data.history['HS-01'], {bestStars:0, completedCount:1}); assert.equal(store.data.completed.length, 1);
  assert.equal(store.data.active.state.stars, 0);
});
test('failed storage pauses the controller and retry preserves its whole state', async () => {
  let fail = true; const c = make(base, async () => { if (fail) throw new Error('disk full'); });
  reveal(c,'cat'); c.answer(false); assert.equal(await c.checkpoint(), false); assert.equal(c.state.paused, true);
  fail = false; assert.equal(await c.checkpoint(), true); c.pause(false); c.continue(); assert.equal(c.state.phase, 'assisted_retry'); assert.equal(c.state.stars, 2);
});
test('offline sync sends decisions, never trusts a client-supplied score, retries idempotently', async () => {
  let online = false; const calls = [];
  const repo = { owner: 'parent', local: false, api: async (path,method,body) => { if (!online) throw new Error('offline'); calls.push({path,body}); return path.endsWith('complete') ? {levelId:'HS-01',bestStars:3,completedCount:1,starsRemaining:3} : [{levelId:'HS-01',bestStars:3,completedCount:1}]; } };
  const store = new HideSeekStore(repo, 'child', bank, memory()); const c = make(base, store.checkpoint);
  reveal(c,'cat'); c.answer(true); c.continue(); await c.checkpoint(); await assert.rejects(store.sync());
  online = true; await store.sync(); await store.sync();
  const completions = calls.filter(c => c.body); assert.equal(completions.length, 1); assert.equal('starsRemaining' in completions[0].body, false); assert.equal(store.data.completed[0].synced, true);
});
