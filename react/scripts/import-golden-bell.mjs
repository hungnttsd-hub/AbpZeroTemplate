import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';

const source = new URL('../../InitialDocs/WordyWings_GoldenBell_1000Q_CodexPack_v1/wordy_golden_bell_pack/data/golden_bell_questions_1000.json', import.meta.url);
const bank = JSON.parse(await readFile(source, 'utf8'));
const types = ['action_recognition','can_cannot','color_object','color_recognition','compare_quantity','count_objects','final_reasoning','function_question','listen_find_picture','memory_scene','missing_letter','multi_clue_scene','odd_one_out','picture_to_word','preposition_scene','select_pair','sentence_completion','sentence_order','shape_recognition','short_story','simple_inference','two_attribute_object','two_step_instruction','weather_choice','word_picture_mismatch'];
const mechanics = ['answer_zone','raise_board','listen_run','quick_match','bell_choice'];
const fail = (q, message) => { throw new Error(`Golden Bell ${q?.id ?? 'bank'}: ${message}`); };
if (bank.count !== 1000 || !Array.isArray(bank.questions) || bank.questions.length !== 1000) fail(null, 'Expected exactly 1000 questions.');
const ids = new Set(), distribution = Array(10).fill(0);
for (const q of bank.questions) {
  if (!/^GB-\d{4}$/.test(q.id) || ids.has(q.id)) fail(q, 'Invalid or duplicated ID.');
  ids.add(q.id);
  if (!Number.isInteger(q.difficulty) || q.difficulty < 1 || q.difficulty > 10) fail(q, 'Difficulty must be 1–10.');
  distribution[q.difficulty - 1]++;
  if (!types.includes(q.questionType) || !mechanics.includes(q.mechanic)) fail(q, 'Unsupported questionType/mechanic.');
  for (const key of ['promptText','audioText','skill','explanation']) if (typeof q[key] !== 'string' || !q[key].trim()) fail(q, `${key} is required.`);
  if (!q.stimulus || !q.hint || !Array.isArray(q.targetVocabulary) || !q.targetVocabulary.length || !Array.isArray(q.worldTags)) fail(q, 'Missing stimulus/hint/vocabulary/worldTags.');
  if (q.estimatedSeconds < 5 || q.estimatedSeconds > 60 || typeof q.requiresMemoryPhase !== 'boolean') fail(q, 'Invalid duration/memory phase.');
  if (new Set(q.options.map(o => o.id)).size !== q.options.length) fail(q, 'Duplicate option IDs.');
  if (q.answer.type === 'option') {
    if (q.options.length < 2 || q.options.length > 4 || !q.options.some(o => o.id === q.answer.value)) fail(q, 'Answer must reference one of 2–4 options.');
  } else if (q.answer.type === 'sequence') {
    if (!Array.isArray(q.answer.value) || !q.answer.value.length || [...q.answer.value].sort().join('|') !== [...q.stimulus.tiles].sort().join('|')) fail(q, 'Tiles must preserve the answer multiset, including repeated words.');
  } else fail(q, 'Unsupported answer type.');
  if (q.questionType === 'missing_letter') {
    const letters = q.stimulus.maskedWord.split(' '), positions = q.answer.missingPositions ?? letters.flatMap((c, i) => c === '_' ? [i] : []);
    const selected = q.options.find(o => o.id === q.answer.value).label;
    if (new Set(positions).size !== positions.length || positions.length !== letters.filter(c => c === '_').length || selected.length !== positions.length || positions.some(i => letters[i] !== '_')) fail(q, 'Invalid missing-letter positions.');
    positions.forEach((position, i) => letters[position] = selected[i]);
    if (letters.join('').toLowerCase() !== q.targetVocabulary[0].toLowerCase()) fail(q, 'Missing letters do not reconstruct the target word.');
  }
  if (q.questionType === 'two_step_instruction' && JSON.stringify(q.options.find(o => o.id === q.answer.value)?.sequence) !== JSON.stringify(q.answer.sequence)) fail(q, 'Two-step answer and selected option disagree.');
}
if (distribution.some(n => n !== 100)) fail(null, 'Expected 100 questions at each difficulty.');
for (let n = 1; n <= 1000; n++) if (!ids.has(`GB-${String(n).padStart(4, '0')}`)) fail(null, `Missing GB-${n}.`);

const output = new URL('../public/content/golden-bell/v1/', import.meta.url);
await mkdir(output, { recursive: true });
const clean = bank.questions.map(q => ({ ...q, options: q.options.map(({ _correct, ...o }) => o) }));
const digest = createHash('sha256').update(JSON.stringify(clean)).digest('hex').slice(0, 16);
const manifest = { version: `1.0-${digest}`, count: 1000, distribution,
  questions: clean.map(({ id, difficulty, questionType, skill, promptText, targetVocabulary, worldTags }) => ({ id, difficulty, questionType, skill, promptText, targetVocabulary, worldTags })) };
// All validation finishes before publication. Individual payloads avoid loading 1000 scenes.
for (const question of clean) await writeFile(new URL(`${question.id}.json`, output), JSON.stringify(question));
await writeFile(new URL('manifest.json', output), JSON.stringify(manifest));
const backend = new URL('../../content/wordy-wings/golden-bell/', import.meta.url);
await mkdir(backend, { recursive: true });
await writeFile(new URL('questions.v1.json', backend), JSON.stringify({ version: manifest.version, count: 1000, questions: clean }));
console.log(`Golden Bell: imported ${ids.size} unique questions / ${types.length} types / 100 per difficulty. Version ${manifest.version}`);
