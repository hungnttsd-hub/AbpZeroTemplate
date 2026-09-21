import { test } from 'node:test';
import assert from 'node:assert/strict';
import { LetterSequenceManager } from '../.word-builder-tests/LetterSequenceManager.js';

const tokens = word => [...word].map((char, i) => ({ id: `letter_${i}`, char }));
for (const word of ['APPLE', 'BOOK', 'GREEN', 'FOOD']) {
  test(`${word}: repeated characters require distinct tokens`, () => {
    const letters = tokens(word); const game = new LetterSequenceManager([...word], letters);
    letters.forEach((t, i) => assert.equal(game.place(t.id, i).ok, true));
    assert.equal(game.complete, true); assert.deepEqual(game.slots, [...word]);
  });
}
test('APPLE: P2 may fill the first P, but cannot also fill the second P', () => {
  const game = new LetterSequenceManager([... 'APPLE'], tokens('APPLE'));
  assert.equal(game.place('letter_2', 1).ok, true);
  assert.deepEqual(game.place('letter_2', 2), { ok: false, reason: 'used' });
  assert.equal(game.slots[2], null);
  assert.equal(game.place('letter_1', 2).ok, true);
});
test('incorrect placement preserves all previous progress and keeps token reusable', () => {
  const game = new LetterSequenceManager([... 'CAT'], tokens('CAT'));
  game.place('letter_0', 0);
  assert.equal(game.place('letter_2', 1).ok, false);
  assert.deepEqual(game.slots, ['C', null, null]);
  assert.equal(game.place('letter_2', 2).ok, true);
});
test('missing letters keep prefilled positions and reject a distractor', () => {
  const game = new LetterSequenceManager([... 'CAT'], [{ id: 'a', char: 'A' }, { id: 'o', char: 'O' }], [0, 2]);
  assert.equal(game.place('o', 1).ok, false); assert.equal(game.complete, false);
  assert.deepEqual(game.place('a', 1), { ok: true, index: 1, complete: true });
});
test('collect mode finds separate free positions for repeated letters', () => {
  const game = new LetterSequenceManager([... 'BOOK'], tokens('BOOK'));
  assert.equal(game.matchingIndex('letter_2'), 1); game.place('letter_2', 1);
  assert.equal(game.matchingIndex('letter_1'), 2);
});
test('malformed inventory is rejected before gameplay', () => {
  assert.throws(() => new LetterSequenceManager([... 'BOOK'], [{ id: 'b', char: 'B' }, { id: 'o', char: 'O' }, { id: 'k', char: 'K' }]));
  assert.throws(() => new LetterSequenceManager(['A', 'A'], [{ id: 'a', char: 'A' }, { id: 'a', char: 'A' }]));
});
test('unknown token, invalid slot and filled slot do not alter the sequence', () => {
  const game = new LetterSequenceManager(['A'], [{ id: 'a', char: 'A' }, { id: 'a2', char: 'A' }]);
  assert.equal(game.place('missing', 0).ok, false); assert.equal(game.place('a', -1).ok, false);
  game.place('a', 0); assert.equal(game.place('a2', 0).ok, false); assert.deepEqual(game.slots, ['A']);
});
test('word units support future sentence building without changing validation', () => {
  const units = ['THE', 'CAT', 'IS', 'BIG']; const game = new LetterSequenceManager(units, units.map((char, i) => ({ id: String(i), char })));
  units.forEach((_, i) => game.place(String(i), i)); assert.equal(game.complete, true);
});
